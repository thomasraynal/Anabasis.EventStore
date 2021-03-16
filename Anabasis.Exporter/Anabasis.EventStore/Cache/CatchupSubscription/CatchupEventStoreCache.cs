using System;
using System.Reactive.Linq;
using EventStore.ClientAPI;
using System.Threading.Tasks;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using System.Linq;
using Anabasis.EventStore.Snapshot;
using DynamicData;

namespace Anabasis.EventStore.Infrastructure.Cache.CatchupSubscription
{
  public class CatchupEventStoreCache<TKey, TAggregate> : BaseCatchupEventStoreCache<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
  {

    private readonly CatchupEventStoreCacheConfiguration<TKey, TAggregate> _catchupEventStoreCacheConfiguration;

    public CatchupEventStoreCache(IConnectionStatusMonitor connectionMonitor,
      CatchupEventStoreCacheConfiguration<TKey, TAggregate> cacheConfiguration,
      IEventTypeProvider eventTypeProvider,
      ISnapshotStore<TKey, TAggregate> snapshotStore = null,
      ISnapshotStrategy<TKey> snapshotStrategy = null,
      ILogger logger = null) : base(connectionMonitor, cacheConfiguration, eventTypeProvider, snapshotStore, snapshotStrategy, logger)
    {
      _catchupEventStoreCacheConfiguration = cacheConfiguration;

      InitializeAndRun();
    }

    protected override void OnResolvedEvent(ResolvedEvent @event)
    {
      var cache = IsCaughtUp ? Cache : CaughtingUpCache;

      UpdateCacheState(@event, cache);
    }

    protected override async Task OnLoadSnapshot()
    {
      if (_catchupEventStoreCacheConfiguration.UseSnapshot)
      {
        var eventTypeFilter = GetEventsFilters();

        var snapshots = await _snapshotStore.Get(eventTypeFilter);

        if (null != snapshots)
        {

          foreach (var snapshot in snapshots)
          {
            Cache.AddOrUpdate(snapshot);
          }

        }

      }
    }

    protected override EventStoreCatchUpSubscription GetEventStoreCatchUpSubscription(IEventStoreConnection connection, Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> onEvent, Action<EventStoreCatchUpSubscription> onCaughtUp, Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped)
    {

      var eventTypeFilter = GetEventsFilters();

      var filter = Filter.EventType.Prefix(eventTypeFilter);

      var subscription = connection.FilteredSubscribeToAllFrom(
        _catchupEventStoreCacheConfiguration.GetSubscribeToAllSubscriptionCheckpoint(),
        filter,
        _catchupEventStoreCacheConfiguration.CatchUpSubscriptionFilteredSettings,
        onEvent,
        onCaughtUp,
        onSubscriptionDropped,
        userCredentials: _eventStoreCacheConfiguration.UserCredentials);

      return subscription;
    }
  }
}
