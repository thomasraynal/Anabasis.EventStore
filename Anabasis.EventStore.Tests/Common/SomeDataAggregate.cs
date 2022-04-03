using Anabasis.Common;
using Anabasis.EventStore.Shared;
using System;

namespace Anabasis.EventStore.Tests
{
    public class AgainSomeMoreData : BaseEvent
    {
        public AgainSomeMoreData(Guid correlationId, string streamId) : base(streamId, correlationId)
        {
        }

    }


    public class SomeMoreData : BaseEvent
    {
        public SomeMoreData(Guid correlationId, string streamId) : base(streamId, correlationId)
        {
        }
    }

    public class SomeData : BaseAggregateEvent<SomeDataAggregate>
    {

        public SomeData(string entityId, Guid correlationId) : base(entityId, correlationId)
        {
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
