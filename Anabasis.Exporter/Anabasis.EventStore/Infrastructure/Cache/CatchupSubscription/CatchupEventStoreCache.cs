using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using EventStore.ClientAPI;
using System.Linq;
using System.Threading.Tasks;
using DynamicData;
using Anabasis.EventStore.Infrastructure;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using Anabasis.EventStore.Infrastructure.Cache;

namespace Anabasis.EventStore
{

  public class CatchupEventStoreCache<TKey, TCacheItem> : BaseEventStoreCache<TKey, TCacheItem> where TCacheItem : IAggregate<TKey>, new()
  {

    private readonly SourceCache<TCacheItem, TKey> _caughtingUpCache = new SourceCache<TCacheItem, TKey>(item => item.EntityId);

    public CatchupEventStoreCache(IConnectionStatusMonitor connectionMonitor,
      IEventStoreCacheConfiguration<TKey, TCacheItem> cacheConfiguration,
      IEventTypeProvider<TKey, TCacheItem> eventTypeProvider,
      ILogger logger = null) : base(connectionMonitor, cacheConfiguration, eventTypeProvider, logger)
    {
    }

    protected override void OnInitialize(IEventStoreConnection connection)
    {

      IsStaleSubject.OnNext(true);

      IsCaughtUpSubject.OnNext(false);

      _eventsConnection.Disposable = ConnectToEventStream(connection)
                                      .Where(ev => CanApply(ev.EventType))
                                      .Subscribe(evt =>
                                      {
                                        var cache = IsCaughtUpSubject.Value ? Cache : _caughtingUpCache;

                                        UpdateCacheState(evt, cache);

                                      });


    }

    private IObservable<RecordedEvent> ConnectToEventStream(IEventStoreConnection connection)
    {

      return Observable.Create<RecordedEvent>(obs =>
      {

        Task onEvent(EventStoreCatchUpSubscription _, ResolvedEvent e)
        {

          obs.OnNext(e.Event);

          return Task.CompletedTask;
        }

        void onCaughtUp(EventStoreCatchUpSubscription evt)
        {

          Cache.Edit(innerCache =>
                        {

                          innerCache.Load(_caughtingUpCache.Items);

                          _caughtingUpCache.Clear();

                        });


          IsCaughtUpSubject.OnNext(true);

          IsStaleSubject.OnNext(false);

        }

        var subscription = connection.SubscribeToAllFrom(Position.Start, CatchUpSubscriptionSettings.Default, onEvent, onCaughtUp, userCredentials: _eventStoreCacheConfiguration.UserCredentials);

        return Disposable.Create(() => subscription.Stop());

      });
    }
  }
}
