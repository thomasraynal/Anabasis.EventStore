using System;

namespace Anabasis.EventStore.Cache
{
    public interface IMultipleStreamsCatchupCacheSubscriptionHolder
    {
        long? CurrentSnapshotVersion { get; }
        bool IsCaughtUp { get; }
        long? LastProcessedEventSequenceNumber { get; }
        DateTime LastProcessedEventUtcTimestamp { get; }
        string StreamId { get; }
    }
}