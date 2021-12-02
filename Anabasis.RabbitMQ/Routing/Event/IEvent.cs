using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public interface IEvent : IAggregateEvent
    {
        string Subject { get; }
    }
}
