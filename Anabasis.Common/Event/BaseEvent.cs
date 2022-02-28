using Anabasis.Common;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

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
            EventId = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
            CorrelationId = correlationId;
            EntityId = streamId;
        }

        [JsonProperty]
        public Guid EventId { get; internal set; }
        [JsonProperty]
        public Guid CorrelationId { get; internal set; }
        [JsonProperty]
        public string EntityId { get; internal set; }
        [JsonProperty]
        public bool IsCommand { get; internal set; }
        [JsonProperty]
        public DateTime Timestamp { get; internal set; }

        public string Name => GetType().Name;

        public abstract Task Ack();

        public abstract Task Nack();
    }
}
