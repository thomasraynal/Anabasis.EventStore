using System;
using EventStore.ClientAPI;
using System.Threading.Tasks;
using Anabasis.EventStore.Snapshot;
using DynamicData;
using Microsoft.Extensions.Logging;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Connection;
using System.Linq;

namespace Anabasis.EventStore.Cache
{
    public class CatchupEventStoreCache<TKey, TAggregate> : BaseCatchupEventStoreCache<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
    {

        private readonly CatchupEventStoreCacheConfiguration<TKey, TAggregate> _catchupEventStoreCacheConfiguration;

        public CatchupEventStoreCache(IConnectionStatusMonitor connectionMonitor,
          CatchupEventStoreCacheConfiguration<TKey, TAggregate> cacheConfiguration,
          IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
          ILoggerFactory loggerFactory,
          ISnapshotStore<TKey, TAggregate> snapshotStore = null,
          ISnapshotStrategy<TKey> snapshotStrategy = null) : base(connectionMonitor, cacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {
            _catchupEventStoreCacheConfiguration = cacheConfiguration;
        }

        protected override EventStoreCatchUpSubscription GetEventStoreCatchUpSubscription(IEventStoreConnection connection, Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> onEvent, Action<EventStoreCatchUpSubscription> onCaughtUp, Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped)
        {

            var eventTypeFilter = GetEventsFilters();

            var filter = Filter.EventType.Prefix(eventTypeFilter);

            Logger?.LogInformation($"{Id} => GetEventStoreCatchUpSubscription - FilteredSubscribeToAllFrom - Filters [{string.Join("|", eventTypeFilter)}]");

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
