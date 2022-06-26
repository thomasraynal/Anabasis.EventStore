using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeezUP2.Framework.Application;
using BeezUP2.Framework.Configuration;
using BeezUP2.Framework.Insights;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.WindowsAzure.Storage.Table;
using OpenTracing;

namespace BeezUP2.Framework.EventHubs
{
    public class SimpleScalingEventHubProcessor : IEventProcessor
    {
        private readonly EventHubProcessorHostParameters _parameters;
        private readonly string _monitoringTableName;
        private readonly ActionAsync<IEnumerable<EventData>> _handleEvents;
        private readonly ILogger _logger;
        private readonly ITracer _tracer;
        private readonly IBeezUPAppKillButton _killButton;
        private CloudTable _table;

        public SimpleScalingEventHubProcessor(
            EventHubProcessorHostParameters parameters,
            string monitoringTableName,
            BeezUPAppContext appContext,
            ActionAsync<IEnumerable<EventData>> handleEvents
        )
        {
            _parameters = parameters;
            _monitoringTableName = monitoringTableName;
            _handleEvents = handleEvents;
            _logger = appContext.Logger;
            _tracer = appContext.Tracer;
            _killButton = appContext;
        }

        void Log(PartitionContext context, string message)
        {
            var id = $"{nameof(SimpleScalingEventHubProcessor)} {_parameters.Connection.Namespace}/{_parameters.Connection.HubName}:{_parameters.ConsumerGroupName} - Partition {context.PartitionId}";
            _logger.LogObject($"{id} ::: {message}");
        }

        public async Task OpenAsync(PartitionContext context)
        {
            Log(context, "initialized");
            _table = await EventHubsHelper.PrepareMonitoringCloudTable(_parameters.EventHubConsumerSettings.TableStorage.GetStorageConnectionString(), _monitoringTableName);
        }

        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            Log(context, $"close (reason : {reason})");
            return Task.CompletedTask;
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            try
            {
                await _handleEvents(messages);
            }
            catch (Exception ex)
            {
                _killButton.KillBeezUPApp("Error while processing EventHubs events", ex);
                throw;
            }

            await context.CheckpointAsync();
            var last = messages.LastOrDefault();
            if (last != null)
            {
                await EventHubsHelper.SaveCheckPointInformationAsync(_table, context, last, null, _parameters.Connection.Namespace);
            }
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            Log(context, $"error -> {error.Message}");
            _logger.LogException(error);
            // _consumedEventDataSubject?.OnError(error); // this line lead to the host process crash !
            return Task.CompletedTask;
        }
    }
}
