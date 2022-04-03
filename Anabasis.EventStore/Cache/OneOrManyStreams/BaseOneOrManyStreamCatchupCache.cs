using Anabasis.Common;
using Anabasis.EventStore.Snapshot;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Reactive.Linq;

namespace Anabasis.EventStore.Cache
{
    public abstract class BaseOneOrManyStreamCatchupCache<TAggregate> : BaseCatchupCache<TAggregate> where TAggregate : class, IAggregate, new()
    {
        protected BaseOneOrManyStreamCatchupCache(IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, MultipleStreamsCatchupCacheConfiguration<TAggregate> catchupCacheConfiguration, IEventTypeProvider<TAggregate> eventTypeProvider, ILoggerFactory loggerFactory, ISnapshotStore<TAggregate>? snapshotStore = null, ISnapshotStrategy? snapshotStrategy = null) : base(connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {
            var catchupCacheSubscriptionHolders = catchupCacheConfiguration.StreamIds.Select(streamId => new CatchupCacheSubscriptionHolder<TAggregate>(streamId, catchupCacheConfiguration.CrashAppIfSubscriptionFail)).ToArray();

            Initialize(catchupCacheSubscriptionHolders);

        }
    }
}
