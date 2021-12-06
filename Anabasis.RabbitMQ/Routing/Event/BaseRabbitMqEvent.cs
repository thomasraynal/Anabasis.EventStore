using Anabasis.RabbitMQ;
using System;

namespace RabbitMQPlayground.Routing.Event
{
    public class BaseRabbitMqEvent : IRabbitMqEvent
    {

        public BaseRabbitMqEvent(Guid eventID, Guid correlationId)
        {
            EventID = eventID;
            CorrelationID = correlationId;
        }

        public string Subject
        {
            get
            {
                return EventRoutingKey.GetRoutingKeyFromEvent(this);
            }
        }

        public Guid EventID { get; }

        public Guid CorrelationID { get; }
    }
}
