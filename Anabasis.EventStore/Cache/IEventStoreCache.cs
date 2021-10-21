using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Shared;
using DynamicData;
using System;

namespace Anabasis.EventStore.Cache
{
    public interface IEventStoreCache<TKey, TAggregate> : IDisposable where TAggregate : IAggregate<TKey>, new()
    {
        string Id { get; }
        bool IsStale { get; }
        bool IsCaughtUp { get; }
        bool IsConnected { get; }
        bool IsWiredUp { get; }
        IObservable<bool> OnConnected { get; }
        IObservable<bool> OnCaughtUp { get; }
        IObservable<bool> OnStale { get; }
        TAggregate GetCurrent(TKey key);
        TAggregate[] GetCurrents();
        IObservableCache<TAggregate, TKey> AsObservableCache();
        IEventTypeProvider<TKey, TAggregate> EventTypeProvider { get; }
        void Connect();
    }
}
