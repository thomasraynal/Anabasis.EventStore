using Anabasis.RabbitMQ.Routing.Bus;
using RabbitMQ.Client.Events;

namespace Anabasis.RabbitMQ
{
    public interface IRabbitMqSubscription
    {
        string SubscriptionId { get; }
        string Exchange { get; }
        string RoutingKey { get; }
        IRabbitMqEventHandler? Subscription { get;  }
        AsyncEventingBasicConsumer Consumer { get; }
        string QueueName { get; }
    }
}