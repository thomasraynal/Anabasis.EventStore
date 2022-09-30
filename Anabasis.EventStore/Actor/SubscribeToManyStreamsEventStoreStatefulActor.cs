using Anabasis.Common;
using Anabasis.EventStore.Factories;
using Anabasis.EventStore.Snapshot;
using DynamicData;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Cache
{
    public abstract class SubscribeToManyStreamsEventStoreStatefulActor<TAggregate> : BaseEventStoreStatefulActor<TAggregate, MultipleStreamsCatchupCacheConfiguration<TAggregate>> where TAggregate : class, IAggregate, new()
    {
        protected SubscribeToManyStreamsEventStoreStatefulActor(IActorConfiguration actorConfiguration, 
            IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, 
            MultipleStreamsCatchupCacheConfiguration<TAggregate> catchupCacheConfiguration, 
            IEventTypeProvider<TAggregate> eventTypeProvider, 
            ILoggerFactory? loggerFactory = null,
            ISnapshotStore<TAggregate>? snapshotStore = null, 
            ISnapshotStrategy? snapshotStrategy = null)
            : base(actorConfiguration, connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {

            var catchupCacheSubscriptionHolders = catchupCacheConfiguration.StreamIds.Select(streamId => new CatchupCacheSubscriptionHolder<TAggregate>(streamId, catchupCacheConfiguration.CrashAppIfSubscriptionFail)).ToArray();

            Initialize(catchupCacheSubscriptionHolders);

        }

        protected SubscribeToManyStreamsEventStoreStatefulActor(IEventStoreActorConfigurationFactory eventStoreActorConfigurationFactory,
            IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, 
            MultipleStreamsCatchupCacheConfiguration<TAggregate> catchupCacheConfiguration, 
            IEventTypeProvider<TAggregate> eventTypeProvider, 
            ILoggerFactory? loggerFactory = null, 
            ISnapshotStore<TAggregate>? snapshotStore = null, 
            ISnapshotStrategy? snapshotStrategy = null, 
            IKillSwitch? killSwitch = null) 
            : base(eventStoreActorConfigurationFactory, connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy, killSwitch)
        {
            var catchupCacheSubscriptionHolders = catchupCacheConfiguration.StreamIds.Select(streamId => new CatchupCacheSubscriptionHolder<TAggregate>(streamId, catchupCacheConfiguration.CrashAppIfSubscriptionFail)).ToArray();

            Initialize(catchupCacheSubscriptionHolders);
        }

        protected override async Task OnLoadSnapshot(
            CatchupCacheSubscriptionHolder<TAggregate>[]? catchupCacheSubscriptionHolders,
            ISnapshotStrategy? snapshotStrategy,
            ISnapshotStore<TAggregate>? snapshotStore)
        {
            if (UseSnapshot)
            {

#nullable disable

                var eventTypeFilter = GetEventsFilters();

                foreach (var catchupCacheSubscriptionHolder in catchupCacheSubscriptionHolders)
                {
                    var snapshot = await snapshotStore.GetByVersionOrLast(catchupCacheSubscriptionHolder.StreamId, eventTypeFilter);

                    if (null == snapshot) continue;

                    catchupCacheSubscriptionHolder.CurrentSnapshotEventVersion = snapshot.VersionFromSnapshot;
                    catchupCacheSubscriptionHolder.LastProcessedEventSequenceNumber = snapshot.VersionFromSnapshot;
                    catchupCacheSubscriptionHolder.LastProcessedEventUtcTimestamp = DateTime.UtcNow;

                    Logger?.LogInformation($"{Id} => OnLoadSnapshot - EntityId: {snapshot.EntityId} StreamId: {snapshot.EntityId}");

                    CurrentCache.AddOrUpdate(snapshot);

                }

#nullable disable

            }
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

            long? subscribeFromPosition = catchupCacheSubscriptionHolder.CurrentSnapshotEventVersion == null ?
                null : catchupCacheSubscriptionHolder.CurrentSnapshotEventVersion;

            Logger?.LogInformation($"{Id} => {nameof(SubscribeToManyStreamsEventStoreStatefulActor<TAggregate>)} - {nameof(GetEventStoreCatchUpSubscription)} - SubscribeToStreamFrom {catchupCacheSubscriptionHolder.StreamId} " +
                $"v.{subscribeFromPosition}]");

            var subscription = connection.SubscribeToStreamFrom(
              catchupCacheSubscriptionHolder.StreamId,
              subscribeFromPosition,
              AggregateCacheConfiguration.CatchUpSubscriptionFilteredSettings,
              onEvent,
              onCaughtUp,
              onSubscriptionDropped,
              userCredentials: AggregateCacheConfiguration.UserCredentials);

            return subscription;
        }

        public Task RemoveEventStoreStreams(params string[] streamIds)
        {
            throw new NotImplementedException();
        }

        public async Task AddEventStoreStreams(params string[] streamIds)
        {
        
            var newStreams = AggregateCacheConfiguration.StreamIds.Concat(streamIds).Distinct().ToArray();

            AggregateCacheConfiguration.StreamIds = newStreams;

            var catchupCacheSubscriptionHolders = AggregateCacheConfiguration.StreamIds.Select(streamId => new CatchupCacheSubscriptionHolder<TAggregate>(streamId, AggregateCacheConfiguration.CrashAppIfSubscriptionFail)).ToArray();

            Initialize(catchupCacheSubscriptionHolders);

            await ConnectToEventStream();

        }

    }
}
