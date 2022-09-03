using Azure;
using Azure.Core;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventHubs
{
    public class AnabasisEventHubProcessor : PluggableCheckpointStoreEventProcessor<EventProcessorPartition>
    {
        public AnabasisEventHubProcessor()
        {
        }

        public AnabasisEventHubProcessor(CheckpointStore checkpointStore, int eventBatchMaximumCount, string consumerGroup, string connectionString, EventProcessorOptions options = null) : base(checkpointStore, eventBatchMaximumCount, consumerGroup, connectionString, options)
        {
        }

        public AnabasisEventHubProcessor(CheckpointStore checkpointStore, int eventBatchMaximumCount, string consumerGroup, string connectionString, string eventHubName, EventProcessorOptions options = null) : base(checkpointStore, eventBatchMaximumCount, consumerGroup, connectionString, eventHubName, options)
        {
        }

        public AnabasisEventHubProcessor(CheckpointStore checkpointStore, int eventBatchMaximumCount, string consumerGroup, string fullyQualifiedNamespace, string eventHubName, AzureNamedKeyCredential credential, EventProcessorOptions options = null) : base(checkpointStore, eventBatchMaximumCount, consumerGroup, fullyQualifiedNamespace, eventHubName, credential, options)
        {
        }

        public AnabasisEventHubProcessor(CheckpointStore checkpointStore, int eventBatchMaximumCount, string consumerGroup, string fullyQualifiedNamespace, string eventHubName, AzureSasCredential credential, EventProcessorOptions options = null) : base(checkpointStore, eventBatchMaximumCount, consumerGroup, fullyQualifiedNamespace, eventHubName, credential, options)
        {
        }

        public AnabasisEventHubProcessor(CheckpointStore checkpointStore, int eventBatchMaximumCount, string consumerGroup, string fullyQualifiedNamespace, string eventHubName, TokenCredential credential, EventProcessorOptions options = null) : base(checkpointStore, eventBatchMaximumCount, consumerGroup, fullyQualifiedNamespace, eventHubName, credential, options)
        {
        }

        protected override Task OnProcessingErrorAsync(Exception exception, EventProcessorPartition partition, string operationDescription, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Task OnProcessingEventBatchAsync(IEnumerable<EventData> events, EventProcessorPartition partition, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
