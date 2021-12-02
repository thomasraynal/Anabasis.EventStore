using RabbitMQPlayground.Routing.Shared;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public abstract class EventSubscriptionBase<TEvent> : IEventSubscription<TEvent> where TEvent : class, IEvent
    {
        public EventSubscriptionBase(string exchange, Expression<Func<TEvent, bool>> routingStrategy)
        {
            var rmqSubjectExpressionVisitor = new RabbitMQSubjectExpressionVisitor(typeof(TEvent));

            rmqSubjectExpressionVisitor.Visit(routingStrategy);

            Exchange = exchange;
            RoutingKey = rmqSubjectExpressionVisitor.Resolve();
        }

        public string Exchange { get; }

        public string RoutingKey { get; }

        public Action<IEvent> OnEvent { get; protected set; }

        public Action<TEvent> OnTypedEvent { get; protected set; }

        public abstract string SubscriptionId { get; }

        public Type EventType => typeof(TEvent);
    }
}
