using System;
using System.Threading.Tasks;
using Anabasis.Common;
using Anabasis.EventStore.Snapshot;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;

namespace Anabasis.EventStore.Cache
{
    public class AllStreamsCatchupCache<TAggregate> : BaseAllStreamsCatchupCache<TAggregate> where TAggregate : class, IAggregate, new()
    {

        private readonly AllStreamsCatchupCacheConfiguration _catchupCacheConfiguration;

        public AllStreamsCatchupCache(IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, AllStreamsCatchupCacheConfiguration catchupCacheConfiguration, IEventTypeProvider eventTypeProvider, ILoggerFactory loggerFactory, ISnapshotStore<TAggregate>? snapshotStore = null, ISnapshotStrategy? snapshotStrategy = null) : base(connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {
            _catchupCacheConfiguration = catchupCacheConfiguration;
        }

        protected override EventStoreCatchUpSubscription GetEventStoreCatchUpSubscription(
            CatchupCacheSubscriptionHolder<TAggregate> catchupCacheSubscriptionHolder,
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

            Logger?.LogInformation($"{Id} => {nameof(AllStreamsCatchupCache<TAggregate>)} - {nameof(GetEventStoreCatchUpSubscription)} - FilteredSubscribeToAllFrom - Filters [{string.Join("|", eventTypeFilter)}]");

            var subscription = connection.FilteredSubscribeToAllFrom(
             _catchupCacheConfiguration.Checkpoint,
             filter,
             _catchupCacheConfiguration.CatchUpSubscriptionFilteredSettings,
             eventAppeared: onEvent,
             liveProcessingStarted: onCaughtUp,
             subscriptionDropped: onSubscriptionDropped,
             userCredentials: _catchupCacheConfiguration.UserCredentials);

            return subscription;
        }

        protected override Task OnLoadSnapshot(CatchupCacheSubscriptionHolder<TAggregate>[]? catchupCacheSubscriptionHolders, ISnapshotStrategy? snapshotStrategy, ISnapshotStore<TAggregate>? snapshotStore)
        {
            return Task.CompletedTask;
        }
    }
}
