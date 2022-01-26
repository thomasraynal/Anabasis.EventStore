using Anabasis.Common;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using Microsoft.Extensions.Logging;

namespace Anabasis.EventStore.Demo
{
    public class MarketDataSink : BaseEventStoreStatefulActor<MarketData>
    {
        public MarketDataSink(IActorConfiguration actorConfiguration, IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<MarketData> eventStoreCache) : base(actorConfiguration,eventStoreRepository, eventStoreCache)
        {
        }

        public MarketDataSink(IActorConfiguration actorConfiguration, IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<MarketData> eventStoreCache, ILoggerFactory loggerFactory = null) : base(actorConfiguration,eventStoreRepository, eventStoreCache, loggerFactory)
        {
        }

        public MarketDataSink(IActorConfiguration actorConfiguration, IEventStoreAggregateRepository eventStoreRepository, IConnectionStatusMonitor connectionStatusMonitor, IEventStoreCacheFactory eventStoreCacheFactory, ILoggerFactory loggerFactory = null) : base(actorConfiguration,eventStoreRepository, connectionStatusMonitor, eventStoreCacheFactory, loggerFactory)
        {
        }
    }
}
