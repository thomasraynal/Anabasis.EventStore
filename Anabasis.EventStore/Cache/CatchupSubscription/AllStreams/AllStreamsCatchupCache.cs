using System;
using System.Threading.Tasks;
using Anabasis.Common;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Snapshot;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;

namespace Anabasis.EventStore.Cache
{
    public class AllStreamsCatchupCache<TKey, TAggregate> : BaseAllStreamsCatchupCache<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
    {

        private readonly AllStreamsCatchupCacheConfiguration<TKey, TAggregate> _catchupCacheConfiguration;

        public AllStreamsCatchupCache(IConnectionStatusMonitor connectionMonitor, AllStreamsCatchupCacheConfiguration<TKey, TAggregate> catchupCacheConfiguration, IEventTypeProvider<TKey, TAggregate> eventTypeProvider, ILoggerFactory loggerFactory, ISnapshotStore<TKey, TAggregate> snapshotStore = null, ISnapshotStrategy<TKey> snapshotStrategy = null) : base(connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {
            _catchupCacheConfiguration = catchupCacheConfiguration;
        }

        protected override EventStoreCatchUpSubscription GetEventStoreCatchUpSubscription(
            CatchupCacheSubscriptionHolder<TKey, TAggregate> catchupCacheSubscriptionHolder,
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

            Logger?.LogInformation($"{Id} => ConnectToEventStream - FilteredSubscribeToAllFrom - Filters [{string.Join("|", eventTypeFilter)}]");

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

        protected override Task OnLoadSnapshot(CatchupCacheSubscriptionHolder<TKey, TAggregate>[] catchupCacheSubscriptionHolders, ISnapshotStrategy<TKey> snapshotStrategy, ISnapshotStore<TKey, TAggregate> snapshotStore)
        {
            return Task.CompletedTask;
        }
    }
}
