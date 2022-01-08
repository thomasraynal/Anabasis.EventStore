using Anabasis.RabbitMQ;
using Anabasis.RabbitMQ.Routing.Bus;
using RabbitMQ.Client.Events;
using System.Collections.Generic;
using System.Reactive.Subjects;

namespace Anabasis.RabbitMQ
{
    public class RabbitMqSubscription : IRabbitMqSubscription
    {

        public RabbitMqSubscription(string exchange, string routingKey, string queueName, EventingBasicConsumer consumer)
        {
            Exchange = exchange;
            RoutingKey = routingKey;

            Subscriptions = new List<IRabbitMqEventHandler>();
            Consumer = consumer;
            QueueName = queueName;

        }
        public string SubscriptionId => $"{Exchange}.{RoutingKey}";
        public string Exchange { get; }
        public string RoutingKey { get; }
        public List<IRabbitMqEventHandler> Subscriptions { get; }
        public EventingBasicConsumer Consumer { get; }
        public string QueueName { get; }
    }

}
