using Anabasis.RabbitMQ.Shared;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ
{

    public class RabbitMqEventSubscription<TEvent> : IRabbitMqEventSubscription<TEvent>
        where TEvent : class, IRabbitMqEvent
    {
        public string Exchange { get; }
        public string RoutingKey { get; protected set; }
        public string SubscriptionId { get; }
        public Func<IRabbitMqQueueMessage, Task> OnMessage { get; }
        public bool IsAutoAck { get; }

        public RabbitMqEventSubscription(string exchange, Func<IRabbitMqQueueMessage, Task> onEvent, bool isAutoAck = true, Expression<Func<TEvent, bool>>? routingStrategy = null)
        {
            if (null == routingStrategy)
                routingStrategy = (_) => true;

            var rabbitMQSubjectExpressionVisitor = new RabbitMQSubjectExpressionVisitor(typeof(TEvent));

            rabbitMQSubjectExpressionVisitor.Visit(routingStrategy);

            IsAutoAck = isAutoAck;
            OnMessage = onEvent;
            RoutingKey = rabbitMQSubjectExpressionVisitor.Resolve();
            Exchange = exchange;

            SubscriptionId = $"{Exchange}.{RoutingKey}";

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
