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
        }

        protected BaseAggregateEvent(string entityId, Guid? correlationId = null, Guid? causeId = null) : base(entityId, correlationId, causeId)
        {
        }

        public abstract void Apply([NotNull] TAggregate entity);

    }
}
