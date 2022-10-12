using Anabasis.Common;
using System;
using System.Collections.Generic;

namespace Anabasis.RabbitMQ
{
    public interface IRabbitMqBus: IBus, IDisposable
    {
        void Emit(IEnumerable<IRabbitMqEvent> events, string exchange, string exchangeType = "topic", TimeSpan? initialVisibilityDelay = null, TimeSpan? expiration = null, bool isMessagePersistent = true, bool isMessageMandatory = false, bool createExchangeIfNotExist = true, (string headerKey, string headerValue)[]? additionalHeaders = null);
        void Emit(IRabbitMqEvent @event, string exchange, string exchangeType = "topic", TimeSpan? initialVisibilityDelay = null, TimeSpan? expiration = null, bool isMessagePersistent = true, bool isMessageMandatory = false, bool createExchangeIfNotExist = true, (string headerKey, string headerValue)[]? additionalHeaders = null);
        IRabbitMqQueueMessage[] Pull(string queueName, bool isAutoAck = false, int? chunkSize = null);
        void SubscribeToExchange<TEvent>(IRabbitMqEventSubscription<TEvent> subscription) where TEvent : class, IRabbitMqEvent;
        void UnsubscribeFromExchange<TEvent>(IRabbitMqEventSubscription<TEvent> subscription) where TEvent : class, IRabbitMqEvent;
    }
}