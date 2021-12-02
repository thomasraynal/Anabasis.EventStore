using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public interface IEventSubscription<TEvent> : IEventSubscription
    {
        Action<TEvent> OnTypedEvent { get; }
    }

    public interface IEventSubscription
    {
        string SubscriptionId { get; }
        string Exchange { get; }
        string RoutingKey { get; }
        Type EventType { get; }
        Action<IEvent> OnEvent { get; }
    }
}
