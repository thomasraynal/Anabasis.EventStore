using System;
using Anabasis.Common;
using Anabasis.Common.Utilities;

namespace Anabasis.EventStore.Samples
{
    public class EventCountOne : BaseAggregateEvent<EventCountAggregate>
    {
        static int count = 0;

        public EventCountOne(int position, string entityId, Guid correlationId) : base(entityId, correlationId)
        {
            Position = position;
        }

        public int Position { get; set; }

        public override void Apply(EventCountAggregate entity)
        {
           if(count++==5) 
                
                throw new Exception("boom");

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
