using Anabasis.RabbitMQ;
using Anabasis.RabbitMQ.Routing.Bus;
using System;
using System.Threading.Tasks;

namespace RabbitMQPlayground.Routing
{
    public interface IRabbitMqEventSubscription<TEvent> : IRabbitMqEventHandler where TEvent: IRabbitMqEvent
    {
        string SubscriptionId { get; }
        string Exchange { get; }
        string RoutingKey { get; }
        Func<TEvent, Task> OnEvent { get; }
    }
}
