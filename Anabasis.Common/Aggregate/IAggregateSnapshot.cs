using System;

namespace Anabasis.Common
{
    public interface IAggregateSnapshot
    {
        string EventFilter { get; set; }
        DateTime LastModifiedUtc { get; set; }
        string SerializedAggregate { get; set; }
        string EntityId { get; set; }
        long Version { get; set; }
    }
}