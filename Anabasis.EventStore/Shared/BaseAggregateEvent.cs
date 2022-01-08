using Anabasis.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Shared
{
    public abstract class BaseAggregateEvent<TEntity> : IEvent, IMutation<TEntity> where TEntity : IAggregate
    {
        public string EntityId { get; set; }
        public Guid EventID { get; set; }
        public Guid CorrelationID { get; set; }
        public bool IsCommand => false;

        [JsonConstructor]
        private protected BaseAggregateEvent()
        {
        }

        protected BaseAggregateEvent(string entityId, Guid correlationId)
        {
            EventID = Guid.NewGuid();
            CorrelationID = correlationId;
            EntityId = entityId;
        }

        public abstract void Apply(TEntity entity);

    }
}
