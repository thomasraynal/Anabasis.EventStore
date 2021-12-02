using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace RabbitMQPlayground.Routing
{
    public class EventSubscription<TEvent> : EventSubscriptionBase<TEvent>, IEventSubscription<TEvent> where TEvent : class, IEvent
    {
        public EventSubscription(string exchange, Expression<Func<TEvent, bool>> routingStrategy, Action<TEvent> onEvent) : base(exchange, routingStrategy)
        {

            OnTypedEvent = onEvent;

            OnEvent = (ev) =>
            {
                if (!(ev is TEvent)) return;

                OnTypedEvent(ev as TEvent);
            };
        }

        public override string SubscriptionId => $"{Exchange}.{RoutingKey}";

        public override bool Equals(object obj)
        {
            return obj is EventSubscription<TEvent> subscription &&
                   SubscriptionId.Equals(subscription.SubscriptionId);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SubscriptionId);
        }
    }
}
