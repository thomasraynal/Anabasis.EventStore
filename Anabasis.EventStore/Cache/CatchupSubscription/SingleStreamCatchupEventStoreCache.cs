using Anabasis.EventStore.Snapshot;
using Microsoft.Extensions.Logging;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;

namespace Anabasis.EventStore.Cache
{
    public class SingleStreamCatchupEventStoreCache<TKey, TAggregate> : MultipleStreamsCatchupCache<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
    {
        public SingleStreamCatchupEventStoreCache(IConnectionStatusMonitor connectionMonitor,
          SingleStreamCatchupEventStoreCacheConfiguration<TKey, TAggregate> cacheConfiguration,
          IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
          ILoggerFactory loggerFactory,
          ISnapshotStore<TKey, TAggregate> snapshotStore = null,
          ISnapshotStrategy<TKey> snapshotStrategy = null) : base(connectionMonitor, cacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {
        }
    }
}
