using Anabasis.EventStore.Snapshot;
using Microsoft.Extensions.Logging;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.Common;

namespace Anabasis.EventStore.Cache
{
    public class SingleStreamCatchupCache< TAggregate> : MultipleStreamsCatchupCache< TAggregate> where TAggregate : IAggregate, new()
    {
        public SingleStreamCatchupCache(IConnectionStatusMonitor connectionMonitor,
          SingleStreamCatchupCacheConfiguration< TAggregate> cacheConfiguration,
          IEventTypeProvider< TAggregate> eventTypeProvider,
          ILoggerFactory loggerFactory,
          ISnapshotStore< TAggregate> snapshotStore = null,
          ISnapshotStrategy snapshotStrategy = null) : base(connectionMonitor, cacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {
        }
    }
}
