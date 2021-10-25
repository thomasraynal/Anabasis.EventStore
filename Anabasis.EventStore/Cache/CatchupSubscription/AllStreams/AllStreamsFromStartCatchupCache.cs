using System;
using EventStore.ClientAPI;
using System.Threading.Tasks;
using Anabasis.EventStore.Snapshot;
using Microsoft.Extensions.Logging;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Connection;

namespace Anabasis.EventStore.Cache
{
    public class AllStreamsFromStartCatchupCache<TKey, TAggregate> : BaseAllStreamsCatchupCache<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
    {

        private readonly AllStreamsFromStartCatchupCacheConfiguration<TKey, TAggregate> _allStreamsCatchupCacheConfiguration;

        public AllStreamsFromStartCatchupCache(IConnectionStatusMonitor connectionMonitor, 
            AllStreamsFromStartCatchupCacheConfiguration<TKey, TAggregate> catchupCacheConfiguration, 
            IEventTypeProvider<TKey, TAggregate> eventTypeProvider, 
            ILoggerFactory loggerFactory, 
            ISnapshotStore<TKey, TAggregate> snapshotStore = null, 
            ISnapshotStrategy<TKey> snapshotStrategy = null) : base(connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {
            _allStreamsCatchupCacheConfiguration = catchupCacheConfiguration;
        }

        protected override EventStoreCatchUpSubscription GetEventStoreCatchUpSubscription(CatchupCacheSubscriptionHolder<TKey, TAggregate> catchupCacheSubscriptionHolder, 
            IEventStoreConnection connection, 
            Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> onEvent, 
            Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped)
        {
            void onCaughtUp(EventStoreCatchUpSubscription _)
            {
                catchupCacheSubscriptionHolder.OnCaughtUpSubject.OnNext(true);
            }

            var eventTypeFilter = GetEventsFilters();

            var filter = Filter.EventType.Prefix(eventTypeFilter);

            Logger?.LogInformation($"{Id} => GetEventStoreCatchUpSubscription - FilteredSubscribeToAllFrom - Filters [{string.Join("|", eventTypeFilter)}]");

            var subscription = connection.FilteredSubscribeToAllFrom(
              _allStreamsCatchupCacheConfiguration.Checkpoint,
              filter,
              _allStreamsCatchupCacheConfiguration.CatchUpSubscriptionFilteredSettings,
              onEvent,
              onCaughtUp,
              onSubscriptionDropped,
              userCredentials: _allStreamsCatchupCacheConfiguration.UserCredentials);

            return subscription;
        }

        protected override Task OnLoadSnapshot(ISnapshotStrategy<TKey> snapshotStrategy, ISnapshotStore<TKey, TAggregate> snapshotStore)
        {
            return Task.CompletedTask;
        }
    }
}
