using Anabasis.Common;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Snapshot;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Reactive.Linq;

namespace Anabasis.EventStore.Cache
{
    public abstract class BaseOneOrManyStreamCatchupCache< TAggregate> : BaseCatchupCache< TAggregate> where TAggregate : class, IAggregate, new()
    {
        protected BaseOneOrManyStreamCatchupCache(IConnectionStatusMonitor connectionMonitor, MultipleStreamsCatchupCacheConfiguration< TAggregate> catchupCacheConfiguration, IEventTypeProvider< TAggregate> eventTypeProvider, ILoggerFactory loggerFactory, ISnapshotStore< TAggregate> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null) : base(connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {
            var catchupCacheSubscriptionHolders = catchupCacheConfiguration.StreamIds.Select(streamId => new CatchupCacheSubscriptionHolder< TAggregate>(streamId)).ToArray();

            Initialize(catchupCacheSubscriptionHolders);

        }
    }
}
