﻿using DynamicData;
using System;
using System.Reactive.Concurrency;

namespace Anabasis.EventStore
{
    public interface IEventStoreCache<TKey, TCacheItem>
    {
        IObservable<bool> IsStale { get; }
        IObservable<bool> IsCaughtUp { get; }
        IObservableCache<TCacheItem, TKey> AsObservableCache();
    }
}