using Anabasis.Common;
using System;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace Anabasis.EventHubs
{

    [DataContract, Serializable]
    public class EventHubOptions : BaseConfiguration
    {

#nullable disable

        [DataMember]
        public TimeSpan CheckPointPeriod { get; set; } = EventHubsConstants.DefaultCheckpointPeriod;

        [Required(AllowEmptyStrings = false)]
        [DataMember]
        public string CheckpointStoreAccountName { get; set; }

        [Required(AllowEmptyStrings = false)]
        [DataMember]
        public string CheckpointStoreAccountKey { get; set; }

        [DataMember]
        public bool CheckpointBlobContainerUseHttps { get; set; } = true;

        [DataMember]
        public string CheckpointBlobContainerName
        {
            get
            {
                return "blob";//$"eh-{EventHubNamespace}-{EventHubName}-checkpoints".ToLower();
            }
        }

        [Required(AllowEmptyStrings = false)]
        [DataMember]
        public string CheckpointTableName { get; set; } = EventHubsConstants.DefaultMonitoringTableName;

        [DataMember]
        public DateTime? EventHubProcessingStartTimeUtcOverride { get; set; }

        [Required(AllowEmptyStrings = false)]
        [DataMember]
        public int EventHubMaximumBatchSize { get; set; } = 100;

        [Required(AllowEmptyStrings = false)]
        [DataMember]
        public string EventHubName { get; set; }

        [Required(AllowEmptyStrings = false)]
        [DataMember]
        public string EventHubConsumerGroup { get; set; } = EventHubsConstants.DefaultConsumerGroupName;

        [Required(AllowEmptyStrings = false)]
        [DataMember]
        public string EventHubNamespace { get; set; }

        [Required(AllowEmptyStrings = false)]
        [DataMember]
        public string EventHubSharedAccessKeyName { get; set; }

        [Required(AllowEmptyStrings = false)]
        [DataMember]
        public string EventHubSharedAccessKey { get; set; }

        [DataMember]
        public bool DoAppCrashOnFailure { get; set; } = true;

#nullable enable
        public string GetCheckpointStorageConnectionString() => $"DefaultEndpointsProtocol=https;AccountName={CheckpointStoreAccountName};AccountKey={CheckpointStoreAccountKey};EndpointSuffix=core.windows.net;UseHttps={CheckpointBlobContainerUseHttps}";

        public string GetEventHubConnectionString() => $"Endpoint=sb://{EventHubNamespace}.servicebus.windows.net/;SharedAccessKeyName={EventHubSharedAccessKeyName};SharedAccessKey={EventHubSharedAccessKey}";

    }
}
