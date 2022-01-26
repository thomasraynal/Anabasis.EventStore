using Anabasis.Common;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using Microsoft.Extensions.Logging;

namespace Anabasis.EventStore.Demo
{
    public class TradeSink : BaseEventStoreStatefulActor<Trade>
    {
        public TradeSink(IActorConfiguration actorConfiguration, IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<Trade> eventStoreCache) : base(actorConfiguration, eventStoreRepository, eventStoreCache)
        {
        }

        public TradeSink(IActorConfiguration actorConfiguration, IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<Trade> eventStoreCache, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, eventStoreCache, loggerFactory)
        {
        }

        public TradeSink(IActorConfiguration actorConfiguration, IEventStoreAggregateRepository eventStoreRepository, IConnectionStatusMonitor connectionStatusMonitor, IEventStoreCacheFactory eventStoreCacheFactory, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, connectionStatusMonitor, eventStoreCacheFactory, loggerFactory)
        {
        }
    }
}
