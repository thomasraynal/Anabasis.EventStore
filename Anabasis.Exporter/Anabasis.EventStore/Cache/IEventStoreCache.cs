using DynamicData;
using System;

namespace Anabasis.EventStore
{
  public interface IEventStoreCache<TKey, TAggregate> : IDisposable where TAggregate : IAggregate<TKey>, new()
  {
    bool IsStale { get; }
    bool IsCaughtUp { get; }
    bool IsConnected { get; }
    IObservable<bool> OnConnected { get; }
    IObservable<bool> OnCaughtUp { get; }
    IObservable<bool> OnStale { get; }
    TAggregate GetCurrent(TKey key);
    TAggregate[] GetCurrents();
    IObservableCache<TAggregate, TKey> AsObservableCache();
  }
}
