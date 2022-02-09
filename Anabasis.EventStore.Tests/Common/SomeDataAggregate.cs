using Anabasis.Common;
using Anabasis.EventStore.Shared;
using System;

namespace Anabasis.EventStore.Tests
{
    public class AgainSomeMoreData : BaseEvent
    {
        public AgainSomeMoreData(Guid correlationId, string streamId) : base(correlationId, streamId)
        {
        }

    }


    public class SomeMoreData : BaseEvent
    {
        public SomeMoreData(Guid correlationId, string streamId) : base(correlationId, streamId)
        {
        }
    }

    public class SomeData : BaseAggregateEvent< SomeDataAggregate>
    {

        public SomeData(string entityId, Guid correlationId) : base(entityId, correlationId)
        {
            EntityId = entityId;
        }

        public override void Apply(SomeDataAggregate entity)
        {
        }
    }

    public class SomeDataAggregate : BaseAggregate
    {

        public SomeDataAggregate(string entityId)
        {
            EntityId = entityId;
        }

        public SomeDataAggregate()
        {
        }
    }
}
