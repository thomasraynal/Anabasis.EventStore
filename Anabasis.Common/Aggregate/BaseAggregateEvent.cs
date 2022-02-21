using Anabasis.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{
    public abstract class BaseAggregateEvent<TAggregate> : BaseEvent, IAggregateEvent<TAggregate> where TAggregate : IAggregate
    {
        [JsonConstructor]
        private protected BaseAggregateEvent()
        {
        }

        protected BaseAggregateEvent(string entityId, Guid correlationId):base(correlationId, entityId)
        {
            Timestamp = DateTime.UtcNow;
            EventID = Guid.NewGuid();
            CorrelationID = correlationId;
            EntityId = entityId;
        }

        public abstract void Apply(TAggregate entity);

    }
}
