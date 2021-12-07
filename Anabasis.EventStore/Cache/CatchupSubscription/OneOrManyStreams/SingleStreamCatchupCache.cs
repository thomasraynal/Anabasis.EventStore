using Anabasis.EventStore.Snapshot;
using Microsoft.Extensions.Logging;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.Common;

namespace Anabasis.EventStore.Cache
{
    public class SingleStreamCatchupCache<TKey, TAggregate> : MultipleStreamsCatchupCache<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
    {
        public SingleStreamCatchupCache(IConnectionStatusMonitor connectionMonitor,
          SingleStreamCatchupCacheConfiguration<TKey, TAggregate> cacheConfiguration,
          IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
          ILoggerFactory loggerFactory,
          ISnapshotStore<TKey, TAggregate> snapshotStore = null,
          ISnapshotStrategy<TKey> snapshotStrategy = null) : base(connectionMonitor, cacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {
        }
    }
}
