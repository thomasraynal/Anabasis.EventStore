using DynamicData;
using System;

namespace Anabasis.EventStore
{
  public interface IEventStoreCache<TKey, TCacheItem>
  {
    bool IsStale { get; }
    bool IsCaughtUp { get; }
    bool IsConnected { get; }
    IObservable<bool> OnConnected { get; }
    IObservable<bool> OnCaughtUp { get; }
    IObservable<bool> OnStale { get; }
    TCacheItem GetCurrent(TKey key);
    TCacheItem[] GetCurrents();
    IObservableCache<TCacheItem, TKey> AsObservableCache();
  }
}
