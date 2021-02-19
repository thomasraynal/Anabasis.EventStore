using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using EventStore.ClientAPI;
using System.Linq;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Anabasis.EventStore.Infrastructure.Cache
{
  public class VolatileEventStoreCache<TKey, TCacheItem> : BaseEventStoreCache<TKey, TCacheItem> where TCacheItem : IAggregate<TKey>, new()
  {

    private VolatileCacheConfiguration<TKey, TCacheItem> _volatileEventStoreCacheConfiguration;

    public VolatileEventStoreCache(IConnectionStatusMonitor connectionMonitor,
      VolatileCacheConfiguration<TKey, TCacheItem> volatileEventStoreCacheConfiguration,
      IEventTypeProvider<TKey, TCacheItem> eventTypeProvider,
      ILogger logger = null) : base(connectionMonitor, volatileEventStoreCacheConfiguration, eventTypeProvider, logger)
    {
      _volatileEventStoreCacheConfiguration = volatileEventStoreCacheConfiguration;

    }

    protected override IObservable<ResolvedEvent> ConnectToEventStream(IEventStoreConnection connection)
    {
      throw new NotImplementedException();
    }

    protected override void OnInitialize(bool isConnected)
    {

      IsCaughtUpSubject.OnNext(true);

      //_eventStreamConnectionDisposable.Disposable = ConnectToEventStream(connection)
      //                                .Where(ev => CanApply(ev.EventType))
      //                                .Subscribe(evt =>
      //                                {
      //                                  UpdateCacheState(evt);

      //                                });
    }

    protected override void OnResolvedEvent(ResolvedEvent @event)
    {
      throw new NotImplementedException();
    }

    //protected override IObservable<RecordedEvent> ConnectToEventStream(IEventStoreConnection connection)
    //{

    //  return Observable.Create<RecordedEvent> (async obs =>
    //  {

    //    Task onEvent(EventStoreSubscription _, ResolvedEvent e)
    //    {

    //      obs.OnNext(e.Event);

    //      return Task.CompletedTask;
    //    }

    //    var subscription = await connection.SubscribeToStreamAsync(_volatileEventStoreCacheConfiguration.StreamId, true, onEvent);

    //    return Disposable.Create(() =>
    //    {

    //      subscription.Unsubscribe();
    //      subscription.Dispose();

    //    });

    //  });
    //}
  }
}
