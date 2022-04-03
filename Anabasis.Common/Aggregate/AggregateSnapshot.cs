using System;
using System.ComponentModel.DataAnnotations;

namespace Anabasis.Common
{
    public class AggregateSnapshot : IAggregateSnapshot
    {

        public AggregateSnapshot(string entityId, string eventFilter, long version, string serializedAggregate, DateTime lastModifiedUtc)
        {
            EntityId = entityId;
            EventFilter = eventFilter;
            Version = version;
            SerializedAggregate = serializedAggregate;
            LastModifiedUtc = lastModifiedUtc;
        }

        [Required]
        public string EntityId { get; set; }
        [Required]
        public string EventFilter { get; set; }
        [Required]
        public long Version { get; set; }
        [Required]
        public DateTime LastModifiedUtc { get; set; }
        [Required]
        [StringLength(4000)]
        public string SerializedAggregate { get; set; }

        public override bool Equals(object obj)
        {
            return obj is AggregateSnapshot snapshot &&
                   EntityId == snapshot.EntityId &&
                   EventFilter == snapshot.EventFilter &&
                   Version == snapshot.Version;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(EntityId, EventFilter, Version);
        }
    }
}
