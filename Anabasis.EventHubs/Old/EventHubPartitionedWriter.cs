using Anabasis.Common;
using Anabasis.EventHubs;
using Anabasis.EventHubs.Old;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.ServiceBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anabasis.Common.Utilities;

namespace BeezUP2.Framework.EventHubs
{
    public interface ISendBatchToEventHubs
    {
        Task SendBatchAsync(IEnumerable<EventData> events, string partitionKey);
    }

    public class EventHubPartitionedWriter : ISendBatchToEventHubs
    {
        private readonly string _connectionSring;
        private readonly string _hubName;
        private readonly Func<string, int, int> _partitionKeyMapper;

        private readonly EventHubClient _eventHubClient;
        private readonly string[] _partitions;
        private readonly int _partitionCount;
        private readonly PartitionSender[] _senders;

        public EventHubPartitionedWriter(EventHubConnectionOptions eventHubConnectionOptions, 
            Func<string, int, int> partitionKeyMapper, 
            AnabasisAppContext appContext)
            : this(eventHubConnectionOptions.GetConnectionString(), eventHubConnectionOptions.HubName, partitionKeyMapper, appContext)
        { }

        public EventHubPartitionedWriter(string connectionSring, 
            string hubName, 
            Func<string, int, int> partitionKeyMapper, 
            AnabasisAppContext appContext)
        {
            _connectionSring = connectionSring;
            _hubName = hubName;

            var conBuilder = new ServiceBusConnectionStringBuilder(connectionSring) { TransportType = Microsoft.Azure.ServiceBus.TransportType.Amqp, EntityPath = hubName };
            _eventHubClient = EventHubClient.CreateFromConnectionString(conBuilder.GetEntityConnectionString());
            _eventHubClient.RetryPolicy = new Microsoft.Azure.EventHubs.RetryExponential(TimeSpan.FromSeconds(10), TimeSpan.FromMinutes(5), 100);

            _partitions = _eventHubClient.GetRuntimeInformationAsync().Result.PartitionIds;
            _partitionCount = _partitions.Length;
            _senders = _partitions.Select(p => _eventHubClient.CreatePartitionSender(p)).ToArray();

            _partitionKeyMapper = partitionKeyMapper ?? new Func<string, int, int>((k, i) => k.GetPartitionId(i));
        }

        public async Task SendBatchAsync(IEnumerable<EventData> events, string partitionKey)
        {
            var partition = _partitionKeyMapper(partitionKey, _partitionCount);
            if (partition < 0 || partition >= _partitionCount)
                throw new IndexOutOfRangeException($"The partition key mapper returned {partition} which is not in the range of partitions expected.");

            var sender = _senders[partition];

            var eventDataBatches = events
                .Batch(MessageConstants.MessageSizeToZip);

            foreach (var batch in eventDataBatches)
            {
                await sender.SendAsync(batch);
            }
        }
    }

}
