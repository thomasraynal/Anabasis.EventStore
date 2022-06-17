using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace BeezUP2.Framework.EventHubs
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
    public class TrackingRecorderCheckpointStatus : TableEntity
    {
        [DataMember]
        public string ConsumerGroupName { get; set; }
        [DataMember]
        public string HostName { get; set; }
        [DataMember]
        public DateTime LastCheckedUtcDate { get; set; }
    }
}
