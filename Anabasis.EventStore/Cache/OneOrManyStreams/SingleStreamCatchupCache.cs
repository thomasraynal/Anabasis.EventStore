using Anabasis.EventStore.Snapshot;
using Microsoft.Extensions.Logging;
using Anabasis.Common;
using EventStore.ClientAPI;

namespace Anabasis.EventStore.Cache
{
    public class SingleStreamCatchupCache<TAggregate> : MultipleStreamsCatchupCache<TAggregate> where TAggregate : class, IAggregate, new()
    {
        public SingleStreamCatchupCache(IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor,
          SingleStreamCatchupCacheConfiguration<TAggregate> cacheConfiguration,
          IEventTypeProvider<TAggregate> eventTypeProvider,
          ILoggerFactory loggerFactory,
          ISnapshotStore<TAggregate>? snapshotStore = null,
          ISnapshotStrategy? snapshotStrategy = null) : base(connectionMonitor, cacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {
        }
    }
}
