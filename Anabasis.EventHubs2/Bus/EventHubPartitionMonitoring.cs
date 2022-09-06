using Azure;
using Azure.Data.Tables;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Primitives;
using System;
using System.Threading.Tasks;

namespace Anabasis.EventHubs.Bus
{
    public class EventHubPartitionMonitoring : IEventHubPartitionMonitoring
    {
        private readonly EventHubOptions _eventHubOptions;
        private readonly TableClient _tableClient;

        public EventHubPartitionMonitoring(EventHubOptions eventHubOptions)
        {
            _eventHubOptions = eventHubOptions;
            _tableClient = GetEventHubMonitoringTableServiceClient();
        }

        private TableClient GetEventHubMonitoringTableServiceClient()
        {
            var storageConnectionString = _eventHubOptions.GetCheckpointStorageConnectionString();

            var tableServiceClient = new TableServiceClient(storageConnectionString);

            tableServiceClient.CreateTableIfNotExists(_eventHubOptions.CheckpointTableName);

            return tableServiceClient.GetTableClient(_eventHubOptions.CheckpointTableName);

        }

        public async Task SaveCheckPointMonitoring(EventProcessorPartition eventProcessorPartition, EventData lastEventProcessed, string hostname, string @namespace)
        {
            TrackingRecorderCheckpointStatus trackingRecorderCheckpointStatus;

            var partitionKey = @namespace + "-" + _eventHubOptions.EventHubNamespace + "-" + _eventHubOptions.EventHubConsumerGroup;
            var rowKey = eventProcessorPartition.PartitionId;

            if (lastEventProcessed == null)
            {
                trackingRecorderCheckpointStatus = new TrackingRecorderCheckpointStatus()
                {
                    PartitionKey = partitionKey,
                    ConsumerGroupName = _eventHubOptions.EventHubConsumerGroup,
                    RowKey = rowKey,
                    HostName = hostname,
                    LastCheckedUtcDate = DateTime.UtcNow,
                };
            }
            else
            {
                long messageRate = 0;
                var duration = TimeSpan.Zero;
                var lastCheckedUtcDate = DateTime.UtcNow;

                var azureResponse = await _tableClient.GetEntityAsync<ExtendedTrackingRecorderCheckpointStatus>(partitionKey, rowKey);
                var fullTrackingRecorderCheckpointStatus = azureResponse.Value;

                long? previousSequenceNumber = null;
                DateTime? previousCheckedUtcDate = null;

                if (fullTrackingRecorderCheckpointStatus != null)
                {
                    previousCheckedUtcDate = fullTrackingRecorderCheckpointStatus.LastCheckedUtcDate;
                    previousSequenceNumber = fullTrackingRecorderCheckpointStatus.SequenceNumber;
                }

                if (previousCheckedUtcDate.HasValue && previousSequenceNumber.HasValue)
                {
                    messageRate = lastEventProcessed.SequenceNumber - previousSequenceNumber.Value;
                    duration = lastCheckedUtcDate - previousCheckedUtcDate.Value;
                }

                var hasEventId = lastEventProcessed.Properties.TryGetValue(EventHubsConstants.EventIdNameInEventProperty, out var eventIdOut);

                if (!(hasEventId && Guid.TryParse(eventIdOut?.ToString(), out var eventId)))
                {
                    eventId = Guid.Empty;
                }

                trackingRecorderCheckpointStatus = new ExtendedTrackingRecorderCheckpointStatus()
                {
                    PartitionKey = partitionKey,
                    RowKey = rowKey,
                    ConsumerGroupName = _eventHubOptions.EventHubConsumerGroup,
                    HostName = hostname,
                    LastEnqueuedUtcDate = lastEventProcessed.EnqueuedTime.UtcDateTime,
                    LastCheckedUtcDate = lastCheckedUtcDate,
                    SequenceNumber = lastEventProcessed.SequenceNumber,
                    PreviousSequenceNumber = previousSequenceNumber,
                    PreviousCheckedUtcDate = previousCheckedUtcDate,
                    MessageRateSincePreviousCheck = messageRate,
                    DurationSincePreviousCheck = duration.ToString("c"),
                    LastEventId = eventId
                };
            }

            await _tableClient.UpdateEntityAsync(trackingRecorderCheckpointStatus, ETag.All, TableUpdateMode.Replace);

        }

    }
}
