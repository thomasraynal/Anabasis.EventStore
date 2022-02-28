using Anabasis.Common;
using Anabasis.RabbitMQ;
using System;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ.Event
{
    public class BaseRabbitMqEvent : IRabbitMqEvent
    {

        public BaseRabbitMqEvent(Guid? messageId, Guid? correlationId)
        {
            EventId = messageId ?? Guid.NewGuid();
            CorrelationId = correlationId ?? Guid.NewGuid();
        }

        public string Subject
        {
            get
            {
                return EventRoutingKey.GetRoutingKeyFromEvent(this);
            }
        }

        public ulong DeliveryTag { get; }

        public Guid EventId { get; }

        public Guid CorrelationId { get; }

        public bool IsCommand => false;

        public string EntityId => Subject;

        public DateTime Timestamp { get; }

        public string Name => GetType().Name;

    }
}
