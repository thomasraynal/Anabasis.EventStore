using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using Microsoft.Extensions.Logging;

namespace Anabasis.EventStore.Demo
{
    public class TradeSink : BaseStatefulActor<long, Trade>
    {
        public TradeSink(IEventStoreAggregateRepository<long> eventStoreRepository, IEventStoreCache<long, Trade> eventStoreCache) : base(eventStoreRepository, eventStoreCache)
        {
        }

        public TradeSink(IEventStoreAggregateRepository<long> eventStoreRepository, IEventStoreCache<long, Trade> eventStoreCache, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, eventStoreCache, loggerFactory)
        {
        }

        public TradeSink(IEventStoreAggregateRepository<long> eventStoreRepository, IConnectionStatusMonitor connectionStatusMonitor, IEventStoreCacheFactory eventStoreCacheFactory, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, connectionStatusMonitor, eventStoreCacheFactory, loggerFactory)
        {
        }
    }
}
