using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public interface ICommand : IAggregateEvent
    {
        string Target { get; }
    }
}
