using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    internal class EventSubscriberDescriptor
    {
        private readonly string _exchange;
        private readonly string _routingKey;

        public EventSubscriberDescriptor(string exchange, string routingKey, EventingBasicConsumer consumer, string queueName)
        {
            _exchange = exchange;
            _routingKey = routingKey;

            Subscriptions = new List<IEventSubscription>();
            Consumer = consumer;
            QueueName = queueName;
        }

        public string SubscriptionId => $"{_exchange}.{_routingKey}";

        public List<IEventSubscription> Subscriptions { get; }
        public EventingBasicConsumer Consumer { get; }
        public string QueueName { get; }
    }

}
