using Anabasis.Common;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Shared;
using DynamicData;
using System;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Cache
{
    public interface IEventStoreCache<TAggregate> : IDisposable where TAggregate : IAggregate, new()
    {
        string Id { get; }
        bool IsStale { get; }
        bool IsCaughtUp { get; }
        bool IsConnected { get; }
        bool IsWiredUp { get; }
        IObservable<bool> OnCaughtUp { get; }
        IObservable<bool> OnStale { get; }
        TAggregate GetCurrent(string key);
        TAggregate[] GetCurrents();
        IObservableCache<TAggregate, string> AsObservableCache();
        IEventTypeProvider<TAggregate> EventTypeProvider { get; }
        Task Connect();
        Task Disconnect();
    }
}
