using Newtonsoft.Json;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Anabasis.Common
{
    public abstract class BaseAggregateEvent<TAggregate> : BaseEvent, IAggregateEvent<TAggregate> where TAggregate : class, IAggregate
    {
        public long EventNumber { get; set; } = -1;

        [JsonConstructor]
        private protected BaseAggregateEvent()
        {
            IsAggregateEvent = true;
        }

        protected BaseAggregateEvent(string entityId, Guid? correlationId = null, Guid? causeId = null) : base(entityId, correlationId, causeId)
        {
            IsAggregateEvent = true;
        }

        public abstract void Apply([NotNull] TAggregate entity);

    }
}
