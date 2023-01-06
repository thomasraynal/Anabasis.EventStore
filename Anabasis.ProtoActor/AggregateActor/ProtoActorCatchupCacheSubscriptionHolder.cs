using Anabasis.EventStore;
using EventStore.ClientAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.AggregateActor
{
    public class ProtoActorCatchupCacheSubscriptionHolder<TAggregate> : IProtoActorCatchupCacheSubscriptionHolder
    {

        public ProtoActorCatchupCacheSubscriptionHolder(string streamId, bool doAppCrashIfSubscriptionFail)
        {
            StreamId = streamId;
            CrashAppIfSubscriptionFail = doAppCrashIfSubscriptionFail;
        }

        public ProtoActorCatchupCacheSubscriptionHolder(string streamId, long lastProcessedEventSequenceNumber, bool doAppCrashIfSubscriptionFail)
        {
            StreamId = streamId;
            LastProcessedEventSequenceNumber = lastProcessedEventSequenceNumber;
            CrashAppIfSubscriptionFail = doAppCrashIfSubscriptionFail;
        }

        public bool CrashAppIfSubscriptionFail { get; private set; }
        public bool IsSuscribeToAll { get; private set; }
        public string StreamId { get; }
        public DateTime LastProcessedEventUtcTimestamp { get; set; }
        public long LastProcessedEventSequenceNumber { get; set; }
        public long? CurrentSnapshotEventVersion { get; set; } = null;

    }
}
