using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Snapshot;
using Microsoft.Extensions.Logging;


namespace Anabasis.EventStore.Cache
{
    public abstract class BaseAllStreamsCatchupCache<TKey, TAggregate> : BaseCatchupCache<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
    {
        public BaseAllStreamsCatchupCache(IConnectionStatusMonitor connectionMonitor, AllStreamsCatchupCacheConfiguration<TKey, TAggregate> catchupCacheConfiguration, IEventTypeProvider<TKey, TAggregate> eventTypeProvider, ILoggerFactory loggerFactory, ISnapshotStore<TKey, TAggregate> snapshotStore = null, ISnapshotStrategy<TKey> snapshotStrategy = null) : base(connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {
            Initialize();
        }
    }
}
