using Newtonsoft.Json;
using System;

namespace Anabasis.Common
{
    public abstract class BaseEvent : IEvent
    {

#nullable disable
        [JsonConstructor]
        private protected BaseEvent()
        {
        }
#nullable enable

        public BaseEvent(string entityId, Guid? correlationId = null, Guid? causeId = null, Guid? traceId = null)
        {
            EventId = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
            CorrelationId = correlationId ?? Guid.NewGuid();
            CauseId = causeId ?? Guid.NewGuid();
            EntityId = entityId;
            TraceId = traceId;
        }

        [JsonProperty]
        public bool IsAggregateEvent { get; internal set; }
        [JsonProperty]
        public Guid EventId { get; internal set; }
        [JsonProperty]
        public Guid CorrelationId { get; internal set; }
        [JsonProperty]
        public Guid? CauseId { get; internal set; }
        [JsonProperty]
        public string EntityId { get; internal set; }
        [JsonProperty]
        public bool IsCommand { get; internal set; }
        [JsonProperty]
        public DateTime Timestamp { get; internal set; }
        [JsonProperty]
        public Guid? TraceId { get; internal set; }
        public string EventName => GetType().Name;

    }
}
