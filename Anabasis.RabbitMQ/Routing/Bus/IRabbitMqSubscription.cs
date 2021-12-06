using Anabasis.RabbitMQ;
using Anabasis.RabbitMQ.Routing.Bus;
using RabbitMQ.Client.Events;
using System.Collections.Generic;

namespace RabbitMQPlayground.Routing
{
    public interface IRabbitMqSubscription
    {
        EventingBasicConsumer Consumer { get; }
        string QueueName { get; }
        string SubscriptionId { get; }
        List<IRabbitMqEventHandler> Subscriptions { get; }
    }
}