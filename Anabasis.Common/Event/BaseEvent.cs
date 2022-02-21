using Anabasis.Common;
using Newtonsoft.Json;
using System;

namespace Anabasis.Common
{
    public abstract class BaseEvent : IEvent
    {
        [JsonConstructor]
        private protected BaseEvent()
        {
        }

        public BaseEvent(Guid correlationId, string streamId)
        {
            EventID = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
            CorrelationID = correlationId;
            EntityId = streamId;
        }

        [JsonProperty]
        public Guid EventID { get; internal set; }
        [JsonProperty]
        public Guid CorrelationID { get; internal set; }
        [JsonProperty]
        public string EntityId { get; internal set; }
        [JsonProperty]
        public bool IsCommand { get; internal set; }
        [JsonProperty]
        public DateTime Timestamp { get; internal set; }

        public string Name => GetType().Name;
    }
}
