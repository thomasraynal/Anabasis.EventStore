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
    public class EventCountStatefulActor : BaseStatefulActor<EventCountAggregate>
    {
        public EventCountStatefulActor(IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<EventCountAggregate> eventStoreCache, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, eventStoreCache, loggerFactory)
        {
        }

        public EventCountStatefulActor(IEventStoreAggregateRepository eventStoreRepository, IConnectionStatusMonitor connectionStatusMonitor, IEventStoreCacheFactory eventStoreCacheFactory, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, connectionStatusMonitor, eventStoreCacheFactory, loggerFactory)
        {
        }

    }
}
