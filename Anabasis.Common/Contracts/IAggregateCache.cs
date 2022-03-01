using DynamicData;
using System;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public interface IAggregateCache<TAggregate> : IDisposable where TAggregate : IAggregate, new()
    {
        string Id { get; }
        bool IsStale { get; }
        bool IsCaughtUp { get; }
        bool IsConnected { get; }
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
