using Anabasis.Common;
using System;
using System.Collections.Generic;

namespace Anabasis.RabbitMQ
{
    public interface IRabbitMqBus: IBus, IDisposable
    {
        IRabbitMqConnection RabbitMqConnection { get; }
        void Emit(IEnumerable<IRabbitMqEvent> events, string exchange, TimeSpan? initialVisibilityDelay = null);
        void Emit(IRabbitMqEvent @event, string exchange, TimeSpan? initialVisibilityDelay = null);
        IRabbitMqQueueMessage[] Pull(string queueName, int? chunkSize = null);
        void Subscribe<TEvent>(IRabbitMqEventSubscription<TEvent> subscription) where TEvent : class, IRabbitMqEvent;
        void Unsubscribe<TEvent>(IRabbitMqEventSubscription<TEvent> subscription) where TEvent : class, IRabbitMqEvent;
    }
}