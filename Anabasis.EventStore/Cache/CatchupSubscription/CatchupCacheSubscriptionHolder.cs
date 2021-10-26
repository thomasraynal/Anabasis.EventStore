﻿using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Anabasis.EventStore.Cache
{
    public class CatchupCacheSubscriptionHolder<TKey, TAggregate> : IDisposable, ICatchupCacheSubscriptionHolder
    {

        private readonly BehaviorSubject<bool> _isCaughtUpSubject;

        internal CatchupCacheSubscriptionHolder()
        {
            IsSuscribeToAll = true;
            _isCaughtUpSubject = new BehaviorSubject<bool>(false);
        }

        internal CatchupCacheSubscriptionHolder(string streamId)
        {
            StreamId = streamId;
            _isCaughtUpSubject = new BehaviorSubject<bool>(false);
        }

        public bool IsSuscribeToAll { get; private set; }
        public bool IsCaughtUp => _isCaughtUpSubject.Value;
        internal BehaviorSubject<bool> OnCaughtUpSubject => _isCaughtUpSubject;
        internal IObservable<bool> OnCaughtUp => _isCaughtUpSubject.AsObservable();
        public string StreamId { get; }
        public DateTime LastProcessedEventUtcTimestamp { get; internal set; }
        internal IDisposable EventStreamConnectionDisposable { get; set; }
        public long? LastProcessedEventSequenceNumber { get; internal set; } = null;
        public long? CurrentSnapshotEventVersion { get; internal set; } = null;
        public void Dispose()
        {
            _isCaughtUpSubject.Dispose();
        }
    }

}