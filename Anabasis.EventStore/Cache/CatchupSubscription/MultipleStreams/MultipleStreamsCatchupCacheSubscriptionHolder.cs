using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Anabasis.EventStore.Cache
{
    public class MultipleStreamsCatchupCacheSubscriptionHolder<TKey, TAggregate> : IDisposable, IMultipleStreamsCatchupCacheSubscriptionHolder
    {
        private readonly BehaviorSubject<bool> _isCaughtUpSubject;

        internal MultipleStreamsCatchupCacheSubscriptionHolder()
        {
            _isCaughtUpSubject = new BehaviorSubject<bool>(false);
        }

        public bool IsCaughtUp => _isCaughtUpSubject.Value;
        internal BehaviorSubject<bool> OnCaughtUpSubject => _isCaughtUpSubject;
        internal IObservable<bool> OnCaughtUp => _isCaughtUpSubject.AsObservable();
        public string StreamId { get; internal set; }
        public DateTime LastProcessedEventUtcTimestamp { get; internal set; }
        internal IDisposable EventStreamConnectionDisposable { get; set; }
        public long? LastProcessedEventSequenceNumber { get; internal set; } = null;
        public long? CurrentSnapshotVersion { get; internal set; } = null;
        public void Dispose()
        {
            _isCaughtUpSubject.Dispose();
        }
    }

}
