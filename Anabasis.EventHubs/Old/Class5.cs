using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventHubs.Old
{
    [Serializable]
    [DataContract]
    public class BeezUPRecordedEvent
    {
        public BeezUPRecordedEvent(long eventNumber, DateTime created, bool isEmptyBatch = true)
        {
            EventNumber = eventNumber;
            Created = created;
            IsEmptyBatch = isEmptyBatch;
        }

        [JsonConstructor]
        public BeezUPRecordedEvent(long? originalEventNumber, string originalStreamId, string eventStreamId, Guid eventId, long eventNumber, string eventType, byte[] data, byte[] metadata, bool isJson, DateTime created, long createdEpoch)
        {
            OriginalEventNumber = originalEventNumber;
            OriginalStreamId = originalStreamId;
            EventStreamId = eventStreamId;
            EventId = eventId;
            EventNumber = eventNumber;
            EventType = eventType;
            Data = data;
            Metadata = metadata;
            IsJson = isJson;
            Created = created;
            CreatedEpoch = createdEpoch;
        }

        [DataMember]
        public long? OriginalEventNumber { get; private set; }

        [DataMember]
        public string OriginalStreamId { get; private set; }

        [DataMember]
        public bool IsEmptyBatch { get; private set; }

        /// <summary>
        /// The Event Stream that this event belongs to
        /// </summary>
        [DataMember]
        public string EventStreamId { get; private set; }

        /// <summary>
        /// The Unique Identifier representing this event
        /// </summary>
        [DataMember]
        public Guid EventId { get; private set; }

        /// <summary>
        /// The number of this event in the stream
        /// </summary>
        [DataMember]
        public long EventNumber { get; private set; }

        /// <summary>
        /// The type of event this is
        /// </summary>
        [DataMember]
        public string EventType { get; private set; }

        /// <summary>
        /// A byte array representing the data of this event
        /// </summary>
        [DataMember]
        public byte[] Data { get; private set; }

        /// <summary>
        /// A byte array representing the metadata associated with this event
        /// </summary>
        [DataMember]
        public byte[] Metadata { get; private set; }

        /// <summary>
        /// Indicates whether the content is internally marked as json
        /// </summary>
        [DataMember]
        public bool IsJson { get; private set; }

        /// <summary>
        /// A datetime representing when this event was created in the system
        /// </summary>
        [DataMember]
        public DateTime Created { get; private set; }

        /// <summary>
        /// A long representing the milliseconds since the epoch when the event was created in the system
        /// </summary>
        [DataMember]
        public long CreatedEpoch { get; private set; }
    }
}
