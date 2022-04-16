using Anabasis.RabbitMQ;
using Anabasis.RabbitMQ.Event;
using Anabasis.RabbitMQ.Routing.Bus;
using System;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ
{
    public interface IRabbitMqEventSubscription: IRabbitMqEventHandler
    {
        string SubscriptionId { get; }
        Func<IRabbitMqQueueMessage, Task> OnMessage { get; }
        IRabbitMqExchangeConfiguration RabbitMqExchangeConfiguration { get; }
        IRabbitMqQueueConfiguration RabbitMqQueueConfiguration { get; }
    }

    public interface IRabbitMqEventSubscription<TEvent> : IRabbitMqEventSubscription where TEvent : IRabbitMqEvent
    {

    }
}
