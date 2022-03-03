using Anabasis.Common;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Factories;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;

namespace Anabasis.EventStore.Samples
{
    public class EventCountStatefulActor : BaseEventStoreStatefulActor<EventCountAggregate>
    {
        public EventCountStatefulActor(IActorConfiguration actorConfiguration, IAggregateCache<EventCountAggregate> eventStoreCache, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreCache, loggerFactory)
        {
        }

        public EventCountStatefulActor(IEventStoreActorConfigurationFactory eventStoreCacheFactory, IConnectionStatusMonitor<IEventStoreConnection> connectionStatusMonitor, ILoggerFactory loggerFactory = null) : base(eventStoreCacheFactory, connectionStatusMonitor, loggerFactory)
        {
        }
    }
}
