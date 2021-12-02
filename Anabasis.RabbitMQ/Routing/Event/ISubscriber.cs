using System;
using System.Collections.Generic;
using System.Text;

namespace RabbitMQPlayground.Routing.Event
{
    public interface ISubscriber
    {
        void Subscribe<TEvent>(IEventSubscription<TEvent> subscribtion);
        void Unsubscribe<TEvent>(IEventSubscription<TEvent> subscribtion);
    }
}
