using Anabasis.Common;
using Anabasis.RabbitMQ.Event;
using Anabasis.RabbitMQ.Shared;
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
            string queueName = "",
            Expression<Func<TEvent, bool>>? routingStrategy = null,
            bool isExchangeDurable = true,
            bool isExchangeAutoDelete = true,
            bool createExchangeIfNotExist = true,
            bool createDeadLetterExchangeIfNotExist = true,
            bool isQueueDurable = false,
            bool isQueueAutoAck = false,
            bool isQueueAutoDelete = true,
            bool isQueueExclusive = true)
        {

            RabbitMqQueueConfiguration = new RabbitMqQueueConfiguration<TEvent>(routingStrategy, 
                queueName: queueName, 
                isDurable: isQueueDurable,
                isExclusive: isQueueExclusive,
                isAutoDelete: isQueueAutoDelete,
                isAutoAck: isQueueAutoAck);

            RabbitMqExchangeConfiguration = new RabbitMqExchangeConfiguration(exchange, 
                exchangeType,
                createExchangeIfNotExist : createExchangeIfNotExist,
                createDeadLetterExchangeIfNotExist : createDeadLetterExchangeIfNotExist,
                isAutoDelete : isExchangeAutoDelete,
                isDurable : isExchangeDurable
                );
            
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
            return OnMessage(rabbitMqQueueMessage);
        }
    }
}
