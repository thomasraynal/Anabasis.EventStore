using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing.Event
{
    public class EventBase : IEvent
    {
        private readonly JsonNetSerializer _serializer;
        private readonly EventSerializer _eventSerializer;

        public EventBase(string aggregateId)
        {
            _serializer = new JsonNetSerializer();
            _eventSerializer = new EventSerializer(_serializer);

            AggregateId = aggregateId;
        }

        [RoutingPosition(0)]
        public string AggregateId { get; }

        public string Subject
        {
            get
            {
                return _eventSerializer.GetRoutingKey(this);
            }
        }

        public Type EventType => this.GetType();
    }
}
