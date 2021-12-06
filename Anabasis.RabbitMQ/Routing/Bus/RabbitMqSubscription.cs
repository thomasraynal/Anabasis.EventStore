using Anabasis.RabbitMQ;
using Anabasis.RabbitMQ.Routing.Bus;
using RabbitMQ.Client.Events;
using System.Collections.Generic;
using System.Reactive.Subjects;

namespace RabbitMQPlayground.Routing
{
    public class RabbitMqSubscription : IRabbitMqSubscription
    {
        private readonly string _exchange;
        private readonly string _routingKey;

        public RabbitMqSubscription(string exchange, string routingKey, string queueName, EventingBasicConsumer consumer)
        {
            _exchange = exchange;
            _routingKey = routingKey;

            Subscriptions = new List<IRabbitMqEventHandler>();
            Consumer = consumer;
            QueueName = queueName;

        }

        public string SubscriptionId => $"{_exchange}.{_routingKey}";

        public List<IRabbitMqEventHandler> Subscriptions { get; }
        public EventingBasicConsumer Consumer { get; }
        public string QueueName { get; }
    }

}
