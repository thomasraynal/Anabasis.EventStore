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

namespace Anabasis.EventStore.Infrastructure.Cache.CatchupSubscription
{

  //todo :handle subscription drop
  //todo : handle checkpoints
  public class SingleStreamCatchupEventStoreCache<TKey, TCacheItem> : BaseCatchupEventStoreCache<TKey, TCacheItem> where TCacheItem : IAggregate<TKey>, new()
  {

    private readonly SingleStreamCatchupEventStoreCacheConfiguration<TKey, TCacheItem> _singleStreamCatchupEventStoreCacheConfiguration;

    public SingleStreamCatchupEventStoreCache(IConnectionStatusMonitor connectionMonitor,
      SingleStreamCatchupEventStoreCacheConfiguration<TKey, TCacheItem> cacheConfiguration,
      IEventTypeProvider<TKey, TCacheItem> eventTypeProvider,
      ILogger logger = null) : base(connectionMonitor, cacheConfiguration, eventTypeProvider, logger)
    {
      _singleStreamCatchupEventStoreCacheConfiguration = cacheConfiguration;

      Run();
    }

    protected override EventStoreCatchUpSubscription GetEventStoreCatchUpSubscription(IEventStoreConnection connection, Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> onEvent, Action<EventStoreCatchUpSubscription> onCaughtUp, Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped)
    {
      var subscription = connection.SubscribeToStreamFrom(
        _singleStreamCatchupEventStoreCacheConfiguration.StreamId,
       null,
        _singleStreamCatchupEventStoreCacheConfiguration.CatchUpSubscriptionSettings,
        onEvent,
        onCaughtUp,
        onSubscriptionDropped,
        userCredentials: _eventStoreCacheConfiguration.UserCredentials);

      return subscription;
    }
  }
}
