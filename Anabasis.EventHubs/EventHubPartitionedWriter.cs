using BeezUP2.Framework.Application;
using BeezUP2.Framework.Business;
using BeezUP2.Framework.Configuration;
using BeezUP2.Framework.Messaging;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.ServiceBus;
using OpenTracing;
using OpenTracing.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeezUP2.Framework.EventHubs
{
    public interface ISendBatchToEventHubs
    {
        Task SendBatchAsync(IEnumerable<EventData> events, string partitionKey);
    }

    public class EventHubPartitionedWriter : ISendBatchToEventHubs
    {
        readonly string _connectionSring;
        readonly string _hubName;
        private readonly BeezUPAppContext _appContext;
        private readonly ITracer _tracer;
        readonly Func<string, int, int> _partitionKeyMapper;

        readonly EventHubClient _eventHubClient;
        readonly string[] _partitions;
        readonly int _partitionCount;
        readonly PartitionSender[] _senders;

        public EventHubPartitionedWriter(EventHubConnectionSettings connectionSettings, BeezUPAppContext appContext = null)
            : this(connectionSettings.GetConnectionString(), connectionSettings.HubName, null, appContext)
        { }

        public EventHubPartitionedWriter(EventHubConnectionSettings connectionSettings, Func<string, int, int> partitionKeyMapper, BeezUPAppContext appContext = null)
            : this(connectionSettings.GetConnectionString(), connectionSettings.HubName, partitionKeyMapper, appContext)
        { }

        public EventHubPartitionedWriter(string connectionSring, string hubName, Func<string, int, int> partitionKeyMapper = null, BeezUPAppContext appContext = null)
        {
            _connectionSring = connectionSring;
            _hubName = hubName;
            _appContext = appContext;
            _tracer = appContext?.Tracer ?? GlobalTracer.Instance;

            var conBuilder = new ServiceBusConnectionStringBuilder(connectionSring) { TransportType = Microsoft.Azure.ServiceBus.TransportType.Amqp, EntityPath = hubName };
            _eventHubClient = EventHubClient.CreateFromConnectionString(conBuilder.GetEntityConnectionString());
            _eventHubClient.RetryPolicy = new Microsoft.Azure.EventHubs.RetryExponential(TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(5), 100);

            _partitions = _eventHubClient.GetRuntimeInformationAsync().Result.PartitionIds;
            _partitionCount = _partitions.Length;
            _senders = _partitions.Select(p => _eventHubClient.CreatePartitionSender(p)).ToArray();

            _partitionKeyMapper = partitionKeyMapper ?? ((k, i) => k.GetPartitionId(i));
        }

        public async Task SendBatchAsync(IEnumerable<EventData> events, string partitionKey)
        {
            var partition = _partitionKeyMapper(partitionKey, _partitionCount);
            if (partition < 0 || partition >= _partitionCount)
                throw new IndexOutOfRangeException($"The partition key mapper returned {partition} which is not in the range of partitions expected.");

            var sender = _senders[partition];

            long totalBatchSize = 0;
            var eventDataBatches = events
                .Select(e =>
                    {
                        _tracer.InjectProperties(e);
                        return e;
                    })
                .Batch(
                    ed => totalBatchSize = ed.Body.Count,
                    ed =>
                    {
                        totalBatchSize += ed.Body.Count;
                        return totalBatchSize < MessageConstants.MessageSizeToZip;
                    });

            foreach (var batch in eventDataBatches)
            {
                await sender.SendAsync(batch).CAF();
            }
        }
    }

}
