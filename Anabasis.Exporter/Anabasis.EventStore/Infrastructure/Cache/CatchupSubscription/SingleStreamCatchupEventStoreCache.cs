using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using EventStore.ClientAPI;
using System.Threading.Tasks;
using DynamicData;
using Anabasis.EventStore.Infrastructure;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Anabasis.EventStore.Infrastructure.Cache;
using System.Linq;

namespace Anabasis.EventStore
{

  //todo : catchup
  //todo :inherit from catchup
  //todo :handle subscription drop
  //todo : handle checkpoints
  public class SingleStreamCatchupEventStoreCache<TKey, TCacheItem> : BaseEventStoreCache<TKey, TCacheItem> where TCacheItem : IAggregate<TKey>, new()
  {

    private readonly SourceCache<TCacheItem, TKey> _caughtingUpCache = new SourceCache<TCacheItem, TKey>(item => item.EntityId);
    private readonly SingleStreamCatchupEventStoreCacheConfiguration<TKey, TCacheItem> _singleStreamCatchupEventStoreCacheConfiguration;

    public SingleStreamCatchupEventStoreCache(IConnectionStatusMonitor connectionMonitor,
      SingleStreamCatchupEventStoreCacheConfiguration<TKey, TCacheItem> cacheConfiguration,
      IEventTypeProvider<TKey, TCacheItem> eventTypeProvider,
      ILogger logger = null) : base(connectionMonitor, cacheConfiguration, eventTypeProvider, logger)
    {
      _singleStreamCatchupEventStoreCacheConfiguration = cacheConfiguration;

      Run();
    }

    protected override void OnDispose()
    {
      _caughtingUpCache.Dispose();
    }

    protected override void OnResolvedEvent(ResolvedEvent @event)
    {

      var cache = IsCaughtUp ? Cache : _caughtingUpCache;

      UpdateCacheState(@event, cache);

    }

    protected override IObservable<ResolvedEvent> ConnectToEventStream(IEventStoreConnection connection)
    {

      return Observable.Create<ResolvedEvent>( obs =>
      {

        Task onEvent(EventStoreCatchUpSubscription _, ResolvedEvent @event)
        {
          obs.OnNext(@event);

          return Task.CompletedTask;
        }

        void onCaughtUp(EventStoreCatchUpSubscription @event)
        {

          Cache.Edit(innerCache =>
                        {

                          innerCache.Load(_caughtingUpCache.Items);

                          _caughtingUpCache.Clear();

                        });


          IsCaughtUpSubject.OnNext(true);

        }

        var eventTypeFilter = _eventTypeProvider.GetAll().Select(type => type.FullName).ToArray();

        var filter = Filter.EventType.Prefix(eventTypeFilter);

        var subscription =  connection.SubscribeToStreamFrom(
          _singleStreamCatchupEventStoreCacheConfiguration.StreamId,
          lastCheckpoint: null,
          _singleStreamCatchupEventStoreCacheConfiguration.CatchUpSubscriptionSettings,
          onEvent,
          onCaughtUp,
          userCredentials: _eventStoreCacheConfiguration.UserCredentials);

        return Disposable.Create(() =>
        {
          subscription.Stop();
        });

      });
    }

  }
}
