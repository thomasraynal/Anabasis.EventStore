using System;

namespace Anabasis.EventStore.Cache
{
    public interface ICatchupCacheSubscriptionHolder
    {
        bool IsSuscribeToAll { get; }
        long? CurrentSnapshotEventVersion { get; }
        bool IsCaughtUp { get; }
        long? LastProcessedEventSequenceNumber { get; }
        DateTime LastProcessedEventUtcTimestamp { get; }
        string StreamId { get; }
    }
}