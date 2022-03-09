using System;
using System.ComponentModel.DataAnnotations;

namespace Anabasis.Common
{
    public class AggregateSnapshot : IAggregateSnapshot
    {
        public AggregateSnapshot()
        {

        }
        public AggregateSnapshot(string streamId, string eventFilter, int version, string serializedAggregate)
        {
            StreamId = streamId;
            EventFilter = eventFilter;
            Version = version;
            SerializedAggregate = serializedAggregate;
        }

        [Required]
        public string StreamId { get; set; }
        [Required]
        public string EventFilter { get; set; }
        [Required]
        public long Version { get; set; }
        [Required]
        public DateTime LastModifiedUtc { get; set; }
        [Required]
        [StringLength(int.MaxValue)]
        public string SerializedAggregate { get; set; }

        public override bool Equals(object obj)
        {
            return obj is AggregateSnapshot snapshot &&
                   StreamId == snapshot.StreamId &&
                   EventFilter == snapshot.EventFilter &&
                   Version == snapshot.Version;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StreamId, EventFilter, Version);
        }
    }
}
