using System;
using EventStore.ClientAPI;
using System.Threading.Tasks;
using Anabasis.EventStore.Snapshot;
using DynamicData;
using Microsoft.Extensions.Logging;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;

namespace Anabasis.EventStore.Cache
{
    public class SingleStreamCatchupEventStoreCache<TKey, TAggregate> : BaseCatchupEventStoreCache<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
    {

        private readonly SingleStreamCatchupEventStoreCacheConfiguration<TKey, TAggregate> _singleStreamCatchupEventStoreCacheConfiguration;

        public SingleStreamCatchupEventStoreCache(IConnectionStatusMonitor connectionMonitor,
          SingleStreamCatchupEventStoreCacheConfiguration<TKey, TAggregate> cacheConfiguration,
          IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
          ILoggerFactory loggerFactory,
          ISnapshotStore<TKey, TAggregate> snapshotStore = null,
          ISnapshotStrategy<TKey> snapshotStrategy = null) : base(connectionMonitor, cacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {
            _singleStreamCatchupEventStoreCacheConfiguration = cacheConfiguration;
        }

        protected override async Task OnLoadSnapshot()
        {
            if (_singleStreamCatchupEventStoreCacheConfiguration.UseSnapshot)
            {
                var eventTypeFilter = GetEventsFilters();

                var snapshot = await _snapshotStore.GetByVersionOrLast(_singleStreamCatchupEventStoreCacheConfiguration.StreamId, eventTypeFilter);

                if (null != snapshot)
                {
                    Logger?.LogInformation($"{Id} => OnLoadSnapshot - EntityId: {snapshot.EntityId} StreamId: {snapshot.StreamId}");

                    var cache = IsCaughtUp ? Cache : CaughtingUpCache;

                    cache.AddOrUpdate(snapshot);
                    LastProcessedEventSequenceNumber = snapshot.Version;
                }

            }
        }


        protected override EventStoreCatchUpSubscription GetEventStoreCatchUpSubscription(IEventStoreConnection connection, Func<EventStoreCatchUpSubscription, ResolvedEvent, Task> onEvent, Action<EventStoreCatchUpSubscription> onCaughtUp, Action<EventStoreCatchUpSubscription, SubscriptionDropReason, Exception> onSubscriptionDropped)
        {

            void onCaughtUpWithCheckpoint(EventStoreCatchUpSubscription _)
            {
                IsCaughtUpSubject.OnNext(true);
            }

            Logger?.LogInformation($"{Id} => GetEventStoreCatchUpSubscription - SubscribeToStreamFrom {_singleStreamCatchupEventStoreCacheConfiguration.StreamId} v.{LastProcessedEventSequenceNumber}]");

            var subscription = connection.SubscribeToStreamFrom(
              _singleStreamCatchupEventStoreCacheConfiguration.StreamId,
             LastProcessedEventSequenceNumber,
              _singleStreamCatchupEventStoreCacheConfiguration.CatchUpSubscriptionSettings,
              onEvent,
              onCaughtUpWithCheckpoint,
              onSubscriptionDropped,
              userCredentials: _eventStoreCacheConfiguration.UserCredentials);

            return subscription;
        }
    }
}
