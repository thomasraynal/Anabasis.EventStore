using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public interface IAggregateEvent
    {
        string AggregateId { get; }
        Type EventType { get; }
    }
}
