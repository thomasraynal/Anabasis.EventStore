using Anabasis.Common;
using Anabasis.EventStore.Snapshot;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Cache
{
    public abstract class BaseOneOrManyStreamEventStoreStatefulActor<TAggregate> : BaseEventStoreStatefulActor<TAggregate, MultipleStreamsCatchupCacheConfiguration<TAggregate>> where TAggregate : class, IAggregate, new()
    {
        protected BaseOneOrManyStreamEventStoreStatefulActor(IActorConfiguration actorConfiguration, 
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
