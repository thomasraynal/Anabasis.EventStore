using Anabasis.RabbitMQ.Routing.Bus;
using RabbitMQ.Client.Events;

namespace Anabasis.RabbitMQ
{
    public class RabbitMqSubscription : IRabbitMqSubscription
    {
 
        public RabbitMqSubscription(string queueName, IRabbitMqEventSubscription rabbitMqEventSubscription, AsyncEventingBasicConsumer consumer)
        {
            Exchange = rabbitMqEventSubscription.RabbitMqExchangeConfiguration.ExchangeName;
            RoutingKey = rabbitMqEventSubscription.RabbitMqQueueConfiguration.RoutingKey;
            Subscription = rabbitMqEventSubscription;
            Consumer = consumer;
            QueueName = queueName;
        }

        public string SubscriptionId => $"{Exchange}.{RoutingKey}";
        public string Exchange { get; }
        public string RoutingKey { get; }
        public IRabbitMqEventHandler Subscription { get; }
        public AsyncEventingBasicConsumer Consumer { get; }
        public string QueueName { get; }
    }

}
