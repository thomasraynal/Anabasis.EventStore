using EventStore.ClientAPI;
using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Anabasis.EventStore
{
    public class CatchupCacheSubscriptionHolder< TAggregate> : IDisposable, ICatchupCacheSubscriptionHolder
    {

        private readonly BehaviorSubject<bool> _isCaughtUpSubject;

        public CatchupCacheSubscriptionHolder(bool doAppCrashIfSubscriptionFail)
        {
            IsSuscribeToAll = true;
            CrashAppIfSubscriptionFail = doAppCrashIfSubscriptionFail;
            _isCaughtUpSubject = new BehaviorSubject<bool>(false);
        }

        public CatchupCacheSubscriptionHolder(string streamId, bool doAppCrashIfSubscriptionFail)
        {
            StreamId = streamId;
            CrashAppIfSubscriptionFail = doAppCrashIfSubscriptionFail;
            _isCaughtUpSubject = new BehaviorSubject<bool>(false);
        }

        public EventStoreCatchUpSubscription? EventStoreCatchUpSubscription { get;  set; }
        public bool CrashAppIfSubscriptionFail { get; private set; }
        public bool IsSuscribeToAll { get; private set; }
        public bool IsCaughtUp => _isCaughtUpSubject.Value;
        public BehaviorSubject<bool> OnCaughtUpSubject => _isCaughtUpSubject;
        public IObservable<bool> OnCaughtUp => _isCaughtUpSubject.AsObservable();
        public string? StreamId { get; }
        public DateTime LastProcessedEventUtcTimestamp { get; set; }
        public IDisposable? EventStreamConnectionDisposable { get; set; }
        public long? LastProcessedEventSequenceNumber { get; set; } = null;
        public long? CurrentSnapshotEventVersion { get; set; } = null;

        public void Dispose()
        {
            _isCaughtUpSubject.Dispose();
        }
    }

}
