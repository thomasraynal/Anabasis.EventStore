using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using Microsoft.Extensions.Logging;

namespace Anabasis.EventStore.Demo
{
    public class TradeSink : BaseEventStoreStatefulActor<Trade>
    {
        public TradeSink(IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<Trade> eventStoreCache) : base(eventStoreRepository, eventStoreCache)
        {
        }

        public TradeSink(IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<Trade> eventStoreCache, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, eventStoreCache, loggerFactory)
        {
        }

        public TradeSink(IEventStoreAggregateRepository eventStoreRepository, IConnectionStatusMonitor connectionStatusMonitor, IEventStoreCacheFactory eventStoreCacheFactory, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, connectionStatusMonitor, eventStoreCacheFactory, loggerFactory)
        {
        }
    }
}
