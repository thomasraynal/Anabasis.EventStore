﻿using Anabasis.Common;
using DynamicData;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Cache
{

    public class MultipleStreamsCatchupCache<TAggregate> : BaseOneOrManyStreamCatchupCache<TAggregate> where TAggregate : class, IAggregate, new()
    {
        private readonly MultipleStreamsCatchupCacheConfiguration _multipleStreamsCatchupCacheConfiguration;

        public MultipleStreamsCatchupCache(IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor,
            MultipleStreamsCatchupCacheConfiguration catchupCacheConfiguration,
            IEventTypeProvider eventTypeProvider,
            ILoggerFactory? loggerFactory = null,
            ISnapshotStore<TAggregate>? snapshotStore = null,
            ISnapshotStrategy? snapshotStrategy = null) : base(connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {
            _multipleStreamsCatchupCacheConfiguration = catchupCacheConfiguration;
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

            Logger?.LogInformation($"{Id} => {nameof(MultipleStreamsCatchupCache<TAggregate>)} - {nameof(GetEventStoreCatchUpSubscription)} - SubscribeToStreamFrom {catchupCacheSubscriptionHolder.StreamId} " +
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
