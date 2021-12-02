using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    internal class CommandSubscriberDescriptor
    {
        public CommandSubscriberDescriptor(EventingBasicConsumer consumer, string exchangeName, string subscriptionId, ICommandSubscription subscription)
        {
            Consumer = consumer;
            ExchangeName = exchangeName;
            Subscription = subscription;
            SubscriptionId = subscriptionId;
        }

        public string SubscriptionId { get; }
        public ICommandSubscription Subscription { get; }
        public EventingBasicConsumer Consumer { get; }
        public string ExchangeName { get; }
    }

}
