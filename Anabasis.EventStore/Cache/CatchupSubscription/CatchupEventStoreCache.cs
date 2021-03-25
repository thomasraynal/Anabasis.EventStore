using System;
using EventStore.ClientAPI;
using System.Threading.Tasks;
using Anabasis.EventStore.Snapshot;
using DynamicData;
using Microsoft.Extensions.Logging;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Connection;

namespace Anabasis.EventStore.Cache
{
  public class CatchupEventStoreCache<TKey, TAggregate> : BaseCatchupEventStoreCache<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
  {

    private readonly CatchupEventStoreCacheConfiguration<TKey, TAggregate> _catchupEventStoreCacheConfiguration;
    private readonly ILogger<CatchupEventStoreCache<TKey, TAggregate>> _logger;

    public CatchupEventStoreCache(IConnectionStatusMonitor connectionMonitor,
      CatchupEventStoreCacheConfiguration<TKey, TAggregate> cacheConfiguration,
      IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
      ISnapshotStore<TKey, TAggregate> snapshotStore = null,
      ISnapshotStrategy<TKey> snapshotStrategy = null,
      ILogger<CatchupEventStoreCache<TKey, TAggregate>> logger = null) : base(connectionMonitor, cacheConfiguration, eventTypeProvider, snapshotStore, snapshotStrategy, logger)
    {
      _catchupEventStoreCacheConfiguration = cacheConfiguration;
      _logger = logger;

      InitializeAndRun();
    }

    protected override void OnResolvedEvent(ResolvedEvent @event)
    {
      var cache = IsCaughtUp ? Cache : CaughtingUpCache;

      _logger?.LogInformation($"OnResolvedEvent => {@event.Event.EventType} - v.{@event.Event.EventNumber} - IsCaughtUp => {IsCaughtUp}");

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
