using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using Microsoft.Extensions.Logging;

namespace Anabasis.EventStore.Demo
{
    public class MarketDataSink : BaseStatefulActor<string, MarketData>
    {
        public MarketDataSink(IEventStoreAggregateRepository<string> eventStoreRepository, IEventStoreCache<string, MarketData> eventStoreCache) : base(eventStoreRepository, eventStoreCache)
        {
        }

        public MarketDataSink(IEventStoreAggregateRepository<string> eventStoreRepository, IEventStoreCache<string, MarketData> eventStoreCache, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, eventStoreCache, loggerFactory)
        {
        }

        public MarketDataSink(IEventStoreAggregateRepository<string> eventStoreRepository, IConnectionStatusMonitor connectionStatusMonitor, IEventStoreCacheFactory eventStoreCacheFactory, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, connectionStatusMonitor, eventStoreCacheFactory, loggerFactory)
        {
        }
    }
}
