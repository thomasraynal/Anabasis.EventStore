using Anabasis.Common;
using Anabasis.RabbitMQ.Event;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ
{

    public class RabbitMqEventSubscription<TEvent> : IRabbitMqEventSubscription<TEvent>
        where TEvent : class, IRabbitMqEvent
    {

        public string SubscriptionId { get; }
        public Func<IRabbitMqQueueMessage, Task> OnMessage { get; }
        public IRabbitMqExchangeConfiguration RabbitMqExchangeConfiguration { get; }
        public IRabbitMqQueueConfiguration RabbitMqQueueConfiguration { get; }

        public RabbitMqEventSubscription(string exchange,
            string exchangeType,
            Func<IRabbitMqQueueMessage, Task> onMessage,
            IActor? actor = null,
            Expression<Func<TEvent, bool>>? routingStrategy = null,
            bool isAutoAck = false,
            bool isAutoDelete= false)
        {

            RabbitMqQueueConfiguration = new RabbitMqQueueConfiguration<TEvent>(routingStrategy, 
                queueName: actor?.Id, 
                isAutoDelete: isAutoDelete,
                isAutoAck: isAutoAck);

            RabbitMqExchangeConfiguration = new RabbitMqExchangeConfiguration(exchange, exchangeType);
            OnMessage = onMessage;

            SubscriptionId = $"{RabbitMqExchangeConfiguration.ExchangeName}-{RabbitMqQueueConfiguration.RoutingKey}";
        }

        public RabbitMqEventSubscription(Func<IRabbitMqQueueMessage, Task> onMessage,
            IRabbitMqQueueConfiguration rabbitMqQueueConfiguration,
            IRabbitMqExchangeConfiguration rabbitMqExchangeConfiguration)
        {

            RabbitMqQueueConfiguration = rabbitMqQueueConfiguration;
            RabbitMqExchangeConfiguration = rabbitMqExchangeConfiguration;
            OnMessage = onMessage;

            SubscriptionId = $"{RabbitMqExchangeConfiguration.ExchangeName}-{RabbitMqQueueConfiguration.RoutingKey}";
        }

        public bool CanHandle(Type eventType)
        {
            return eventType == typeof(TEvent);
        }

        public Task Handle(IRabbitMqQueueMessage rabbitMqQueueMessage)
        {
            var eventType = rabbitMqQueueMessage.Content.GetType();

            if (!CanHandle(eventType))
                throw new InvalidOperationException($"{SubscriptionId} cannot handle event {eventType}");

            return OnMessage(rabbitMqQueueMessage);
        }
    }
}
