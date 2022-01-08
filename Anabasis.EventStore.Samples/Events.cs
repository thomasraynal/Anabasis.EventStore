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
    public class EventCountOne : BaseAggregateEvent<EventCountAggregate>
    {
        public EventCountOne(int position, string entityId, Guid correlationId) : base(entityId, correlationId)
        {
            Position = position;
        }

        public int Position { get; set; }

        public override void Apply(EventCountAggregate entity)
        {
            entity.HitCounter += 1;
        }
    }

    public class EventCountTwo : BaseAggregateEvent<EventCountAggregate>
    {
        public EventCountTwo(int position, string entityId, Guid correlationId) : base(entityId, correlationId)
        {
            Position = position;
        }

        public int Position { get; set; }

        public override void Apply(EventCountAggregate entity)
        {
            entity.HitCounter += 1;
        }
    }
}
