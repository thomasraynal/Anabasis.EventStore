using System;

namespace Anabasis.ProtoActor.AggregateActor
{
    public interface IProtoActorCatchupCacheSubscriptionHolder
    {
        bool CrashAppIfSubscriptionFail { get; }
        long? CurrentSnapshotEventVersion { get; set; }
        bool IsSuscribeToAll { get; }
        long     LastProcessedEventSequenceNumber { get; set; }
        DateTime LastProcessedEventUtcTimestamp { get; set; }
        string StreamId { get; }
    }
}