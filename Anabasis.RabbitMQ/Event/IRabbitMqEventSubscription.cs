using Anabasis.RabbitMQ;
using Anabasis.RabbitMQ.Routing.Bus;
using System;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ
{
    public interface IRabbitMqEventSubscription<TEvent> : IRabbitMqEventHandler where TEvent : IRabbitMqEvent
    {
        string SubscriptionId { get; }
        string Exchange { get; }
        string RoutingKey { get; }
        bool IsAutoAck { get; }
        Func<IRabbitMqQueueMessage, Task> OnMessage { get; }
    }
}
