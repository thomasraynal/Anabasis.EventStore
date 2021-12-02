using RabbitMQPlayground.Routing.Event;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public interface IBus : IActor, IPublisher, ISubscriber, ICommandHandler
    { 
    }
}
