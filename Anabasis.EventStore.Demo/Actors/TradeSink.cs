using Anabasis.Common;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.AspNet.Factories;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;

namespace Anabasis.EventStore.Demo
{
    public class TradeSink : BaseEventStoreStatefulActor<Trade>
    {
        public TradeSink(IActorConfiguration actorConfiguration, IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<Trade> eventStoreCache, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, eventStoreCache, loggerFactory)
        {
        }

        public TradeSink(IEventStoreActorConfigurationFactory eventStoreCacheFactory, IEventStoreAggregateRepository eventStoreRepository, IConnectionStatusMonitor<IEventStoreConnection> connectionStatusMonitor, ILoggerFactory loggerFactory = null) : base(eventStoreCacheFactory, eventStoreRepository, connectionStatusMonitor, loggerFactory)
        {
        }
    }
}
