using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventHubs
{

    [DataContract, Serializable]
    public class EventHubOptions : BaseConfiguration
    {

#nullable disable

        [DataMember]
        public TimeSpan CheckPointPeriod { get; set; } = EventHubsConstants.DefaultCheckpointPeriod;

        [DataMember]
        public string CheckpointStoreAccountName { get; set; }

        [DataMember]
        public string CheckpointStoreAccountKey { get; set; }

        [DataMember]
        public bool CheckpointBlobContainerUseHttps { get; set; } = true;

        [DataMember]
        public string CheckpointBlobContainerName
        {
            get
            {
                return $"eh-{EventHubNamespace}-{EventHubName}-checkpoints";
            }
        }

        [DataMember]
        public string CheckpointTableName { get; set; } = EventHubsConstants.DefaultMonitoringTableName;

        [DataMember]
        public DateTime? EventHubProcessingStartTimeUtcOverride { get; set; }

        [DataMember]
        public int EventHubMaximumBatchSize { get; set; } = 100;

        [DataMember]
        public string EventHubName { get; set; }

        [DataMember]
        public string EventHubConsumerGroup { get; set; } = EventHubsConstants.DefaultConsumerGroupName;

        [DataMember]
        public string EventHubNamespace { get; set; }

        [DataMember]
        public string EventHubSharedAccessKeyName { get; set; }

        [DataMember]
        public string EventHubSharedAccessKey { get; set; }

        [DataMember]
        public bool DoAppCrashOnFailure { get; set; } = true;

#nullable enable
        public string GetCheckpointStorageConnectionString() => $"DefaultEndpointsProtocol=https;AccountName={CheckpointStoreAccountName};AccountKey={CheckpointStoreAccountKey};EndpointSuffix=core.windows.net;UseHttps={CheckpointBlobContainerUseHttps}";

        public string GetEventHubConnectionString() => $"Endpoint=sb://{EventHubNamespace}.servicebus.windows.net/;SharedAccessKeyName={EventHubSharedAccessKeyName};SharedAccessKey={EventHubSharedAccessKey}";

    }
}
