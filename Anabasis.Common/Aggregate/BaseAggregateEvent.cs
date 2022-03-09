using Anabasis.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{
    public abstract class BaseAggregateEvent<TAggregate> : BaseEvent, IAggregateEvent<TAggregate> where TAggregate : class, IAggregate
    {
        public long EventNumber { get; set; } = -1;

        [JsonConstructor]
        private protected BaseAggregateEvent()
        {
        }

        protected BaseAggregateEvent(string entityId, Guid correlationId):base(correlationId, entityId)
        {
            Timestamp = DateTime.UtcNow;
            EventId = Guid.NewGuid();
            CorrelationId = correlationId;
            EntityId = entityId;
        }

        public abstract void Apply(TAggregate entity);

    }
}
