using Anabasis.RabbitMQ;
using System;
using System.Collections.Generic;

namespace RabbitMQPlayground.Routing
{
    public interface IBus : IDisposable
    {
        string Id { get; }
        void Emit(IEnumerable<IRabbitMqEvent> events, string exchange, TimeSpan? initialVisibilityDelay = null);
        void Emit(IRabbitMqEvent @event, string exchange, TimeSpan? initialVisibilityDelay = null);
        IRabbitMqSubscription Subscribe(IRabbitMqEventSubscription subscription);
        void Unsubscribe<TEvent>(IRabbitMqEventSubscription subscription);
    }
}