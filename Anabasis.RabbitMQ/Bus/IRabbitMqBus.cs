using Anabasis.Common;
using System;
using System.Collections.Generic;

namespace Anabasis.RabbitMQ
{
    public interface IRabbitMqBus: IBus, IDisposable
    {
        IRabbitMqConnection RabbitMqConnection { get; }
        void Emit(IEnumerable<IRabbitMqMessage> events, string exchange, TimeSpan? initialVisibilityDelay = null);
        void Emit(IRabbitMqMessage @event, string exchange, TimeSpan? initialVisibilityDelay = null);
        IRabbitMqQueueMessage[] Pull(string queueName, int? chunkSize = null);
        void Subscribe<TEvent>(IRabbitMqEventSubscription<TEvent> subscription) where TEvent : class, IRabbitMqMessage;
        void Unsubscribe<TEvent>(IRabbitMqEventSubscription<TEvent> subscription) where TEvent : class, IRabbitMqMessage;
    }
}