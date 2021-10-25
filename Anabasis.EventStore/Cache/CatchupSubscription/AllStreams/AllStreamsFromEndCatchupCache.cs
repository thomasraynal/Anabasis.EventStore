using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Snapshot;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;

namespace Anabasis.EventStore.Cache
{
    public class AllStreamsFromEndCatchupCache<TKey, TAggregate> : BaseAllStreamsCatchupCache<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
    {

        private readonly AllStreamsFromEndCatchupCacheConfiguration<TKey, TAggregate> _catchupCacheConfiguration;

        public AllStreamsFromEndCatchupCache(IConnectionStatusMonitor connectionMonitor, AllStreamsFromEndCatchupCacheConfiguration<TKey, TAggregate> catchupCacheConfiguration, IEventTypeProvider<TKey, TAggregate> eventTypeProvider, ILoggerFactory loggerFactory, ISnapshotStore<TKey, TAggregate> snapshotStore = null, ISnapshotStrategy<TKey> snapshotStrategy = null) : base(connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
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

            var eventTypeFilter = EventTypeProvider.GetAll().Select(type => type.FullName).ToArray();

            var filter = Filter.EventType.Prefix(eventTypeFilter);

            Logger?.LogInformation($"{Id} => ConnectToEventStream - FilteredSubscribeToAllFrom - Filters [{string.Join("|", eventTypeFilter)}]");

            var subscription = connection.FilteredSubscribeToAllFrom(
             Position.End,
             filter,
             _catchupCacheConfiguration.CatchUpSubscriptionFilteredSettings,
             eventAppeared: onEvent,
             liveProcessingStarted: onCaughtUp,
             subscriptionDropped: onSubscriptionDropped,
             userCredentials: _catchupCacheConfiguration.UserCredentials);

            return subscription;
        }

        protected override Task OnLoadSnapshot(ISnapshotStrategy<TKey> snapshotStrategy, ISnapshotStore<TKey, TAggregate> snapshotStore)
        {
            return Task.CompletedTask;
        }
    }
}
