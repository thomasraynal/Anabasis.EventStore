using System;

namespace Anabasis.Common
{
    public interface IAggregateSnapshot
    {
        string EventFilter { get; set; }
        DateTime LastModifiedUtc { get; set; }
        string SerializedAggregate { get; set; }
        string StreamId { get; set; }
        long Version { get; set; }
    }
}