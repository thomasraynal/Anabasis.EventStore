using Anabasis.Common;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Samples
{
    public class EventCountStatefulActor : BaseEventStoreStatefulActor<EventCountAggregate>
    {
        public EventCountStatefulActor(IActorConfiguration actorConfiguration, IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<EventCountAggregate> eventStoreCache, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, eventStoreCache, loggerFactory)
        {
        }

        public EventCountStatefulActor(IActorConfiguration actorConfiguration, IEventStoreAggregateRepository eventStoreRepository, IConnectionStatusMonitor connectionStatusMonitor, IEventStoreCacheFactory eventStoreCacheFactory, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, connectionStatusMonitor, eventStoreCacheFactory, loggerFactory)
        {
        }

    }
}
