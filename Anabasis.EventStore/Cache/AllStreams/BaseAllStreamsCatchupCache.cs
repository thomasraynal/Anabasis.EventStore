using Anabasis.Common;

using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;


namespace Anabasis.EventStore.Cache
{
    public abstract class BaseAllStreamsCatchupCache< TAggregate> : BaseCatchupCache< TAggregate> where TAggregate :class, IAggregate, new()
    {
        public BaseAllStreamsCatchupCache(IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, AllStreamsCatchupCacheConfiguration catchupCacheConfiguration, IEventTypeProvider eventTypeProvider, ILoggerFactory loggerFactory, ISnapshotStore< TAggregate>? snapshotStore = null, ISnapshotStrategy? snapshotStrategy = null) : base(connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {
            Initialize();
        }
    }
}
