using Azure;
using Azure.Data.Tables;
using System;
using System.Runtime.Serialization;

namespace Anabasis.EventHubs
{
    [DataContract, Serializable]
    public class FullTrackingRecorderCheckpointStatus : TrackingRecorderCheckpointStatus
    {
        [DataMember]
        public DateTime? LastEnqueuedUtcDate { get; set; }
        [DataMember]
        public long SequenceNumber { get; set; }
        [DataMember]
        public long? PreviousSequenceNumber { get; set; }
        [DataMember]
        public DateTime? PreviousCheckedUtcDate { get; set; }
        [DataMember]
        public long? MessageRateSincePreviousCheck { get; set; }
        [DataMember]
        public string DurationSincePreviousCheck { get; set; }
        [DataMember]
        public Guid? LastEventId { get; set; }
    }

    [DataContract, Serializable]
    public class TrackingRecorderCheckpointStatus : ITableEntity
    {
        [DataMember]
        public string ConsumerGroupName { get; set; }
        [DataMember]
        public string HostName { get; set; }
        [DataMember]
        public DateTime LastCheckedUtcDate { get; set; }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
