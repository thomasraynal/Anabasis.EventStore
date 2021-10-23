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

namespace Anabasis.EventStore.Demo2
{

    public class EventCountOne : BaseAggregateEvent<string, EventCountAggregate>
    {
        public EventCountOne(int position, string entityId, Guid correlationId) : base(entityId, correlationId)
        {
            Position = position;
        }

        public int Position { get;  set; }

        protected override void ApplyInternal(EventCountAggregate entity)
        {
            entity.HitCounter += 1;
        }
    }

    public class EventCountTwo : BaseAggregateEvent<string, EventCountAggregate>
    {
        public EventCountTwo(int position, string entityId, Guid correlationId) : base(entityId, correlationId)
        {
            Position = position;
        }

        public int Position { get; set; }

        protected override void ApplyInternal(EventCountAggregate entity)
        {
            entity.HitCounter += 1;
        }
    }

    public class EventCountAggregate : BaseAggregate<string>
    {
        public EventCountAggregate()
        {
        }

        public int HitCounter { get; set; }

        public void PrintConsole()
        {
            var header = $"{nameof(EventCountAggregate)} - {StreamId} - {HitCounter}";

            Console.WriteLine(header);

            var groupedEvents = AppliedEvents.GroupBy(ev => ev.GetType().Name);

            foreach(var events in groupedEvents)
            {
                Console.WriteLine($"    {events.Key} : {events.Count()}");

            }

        }
    }

    public class EventCountActor : BaseStatefulActor<string, EventCountAggregate>
    {
        public EventCountActor(IEventStoreAggregateRepository<string> eventStoreRepository, IEventStoreCache<string, EventCountAggregate> eventStoreCache, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, eventStoreCache, loggerFactory)
        {
        }

        public EventCountActor(IEventStoreAggregateRepository<string> eventStoreRepository, IConnectionStatusMonitor connectionStatusMonitor, IEventStoreCacheFactory eventStoreCacheFactory, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, connectionStatusMonitor, eventStoreCacheFactory, loggerFactory)
        {
        }

    }
}
