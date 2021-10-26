using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Snapshot;
using DynamicData;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Cache
{

    public class MultipleStreamsCatchupCache<TKey, TAggregate> : BaseOneOrManyStreamCatchupCache<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
    {
        private MultipleStreamsCatchupCacheConfiguration<TKey, TAggregate> _multipleStreamsCatchupCacheConfiguration;

        public MultipleStreamsCatchupCache(IConnectionStatusMonitor connectionMonitor, 
            MultipleStreamsCatchupCacheConfiguration<TKey, TAggregate> catchupCacheConfiguration, 
            IEventTypeProvider<TKey, TAggregate> eventTypeProvider, 
            ILoggerFactory loggerFactory, 
            ISnapshotStore<TKey, TAggregate> snapshotStore = null, 
            ISnapshotStrategy<TKey> snapshotStrategy = null) : base(connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {
            _multipleStreamsCatchupCacheConfiguration = catchupCacheConfiguration;
        }

        protected override async Task OnLoadSnapshot(
            CatchupCacheSubscriptionHolder<TKey, TAggregate>[] catchupCacheSubscriptionHolders,
            ISnapshotStrategy<TKey> snapshotStrategy, 
            ISnapshotStore<TKey,TAggregate> snapshotStore)
        {
            if (UseSnapshot)
            {
                var eventTypeFilter = GetEventsFilters();

                foreach (var catchupCacheSubscriptionHolder in catchupCacheSubscriptionHolders)
                {
                    var snapshot = await snapshotStore.GetByVersionOrLast(catchupCacheSubscriptionHolder.StreamId, eventTypeFilter);

                    if (null == snapshot) continue;

                    catchupCacheSubscriptionHolder.CurrentSnapshotEventVersion = snapshot.VersionFromSnapshot;
                    catchupCacheSubscriptionHolder.LastProcessedEventSequenceNumber = snapshot.VersionFromSnapshot;
                    catchupCacheSubscriptionHolder.LastProcessedEventUtcTimestamp = DateTime.UtcNow;

                    Logger?.LogInformation($"{Id} => OnLoadSnapshot - EntityId: {snapshot.EntityId} StreamId: {snapshot.StreamId}");

                    CurrentCache.AddOrUpdate(snapshot);

                }
            }
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

            long? subscribeFromPosition = catchupCacheSubscriptionHolder.CurrentSnapshotEventVersion == null ?
                null : catchupCacheSubscriptionHolder.CurrentSnapshotEventVersion;

            Logger?.LogInformation($"{Id} => GetEventStoreCatchUpSubscription - SubscribeToStreamFrom {catchupCacheSubscriptionHolder.StreamId} " +
                $"v.{subscribeFromPosition}]");

            var subscription = connection.SubscribeToStreamFrom(
              catchupCacheSubscriptionHolder.StreamId,
              subscribeFromPosition,
              _multipleStreamsCatchupCacheConfiguration.CatchUpSubscriptionFilteredSettings,
              onEvent,
              onCaughtUp,
              onSubscriptionDropped,
              userCredentials: _multipleStreamsCatchupCacheConfiguration.UserCredentials);

            return subscription;
        }
    }
}
