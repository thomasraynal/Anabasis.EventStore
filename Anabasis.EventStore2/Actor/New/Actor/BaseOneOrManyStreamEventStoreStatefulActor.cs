using Anabasis.Common;
using Anabasis.EventStore.Snapshot;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Cache
{
    public abstract class BaseOneOrManyStreamEventStoreStatefulActor<TAggregate> : BaseEventStoreStatefulActor<TAggregate> where TAggregate : class, IAggregate, new()
    {
        protected BaseOneOrManyStreamEventStoreStatefulActor(IActorConfiguration actorConfiguration, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, MultipleStreamsCatchupCacheConfiguration<TAggregate> catchupCacheConfiguration, IEventTypeProvider<TAggregate> eventTypeProvider, ILoggerFactory? loggerFactory = null, ISnapshotStore<TAggregate>? snapshotStore = null, ISnapshotStrategy? snapshotStrategy = null)
            : base(actorConfiguration, connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {
            var catchupCacheSubscriptionHolders = catchupCacheConfiguration.StreamIds.Select(streamId => new CatchupCacheSubscriptionHolder<TAggregate>(streamId, catchupCacheConfiguration.CrashAppIfSubscriptionFail)).ToArray();

            Initialize(catchupCacheSubscriptionHolders);

        }

        public async Task RemoveEventStoreStreams(params string[] streamIds)
        {

        }

        public async Task AddEventStoreStreams(params string[] streamIds)
        {
            var multipleStreamsCatchupCacheConfiguration = _catchupCacheConfiguration as MultipleStreamsCatchupCacheConfiguration<TAggregate>;
            var newStreams = multipleStreamsCatchupCacheConfiguration.StreamIds.Concat(streamIds).Distinct().ToArray();

            multipleStreamsCatchupCacheConfiguration.StreamIds = newStreams;

            var catchupCacheSubscriptionHolders = multipleStreamsCatchupCacheConfiguration.StreamIds.Select(streamId => new CatchupCacheSubscriptionHolder<TAggregate>(streamId, multipleStreamsCatchupCacheConfiguration.CrashAppIfSubscriptionFail)).ToArray();

            Initialize(catchupCacheSubscriptionHolders);

            await ConnectToEventStream();

        }

    }
}
