using Anabasis.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ
{
    public interface IRabbitMqBus : IBus, IDisposable
    {
        void Emit(IEnumerable<IRabbitMqEvent> events, string exchange, string exchangeType, TimeSpan? initialVisibilityDelay = null, TimeSpan? expiration = null, bool isPersistent = true, bool isMandatory = false, (string headerKey, string headerValue)[]? additionalHeaders = null);
        void Emit(IRabbitMqEvent @event, string exchange, string exchangeType, TimeSpan? initialVisibilityDelay = null, TimeSpan? expiration = null, bool isPersistent = true, bool isMandatory = false, (string headerKey, string headerValue)[]? additionalHeaders = null);
        IRabbitMqQueueMessage[] Pull(string queueName, bool isAutoAck = false, int? chunkSize = null);
        void SubscribeToExchange<TEvent>(IRabbitMqEventSubscription<TEvent> subscription) where TEvent : class, IRabbitMqEvent;
        void UnsubscribeFromExchange<TEvent>(IRabbitMqEventSubscription<TEvent> subscription) where TEvent : class, IRabbitMqEvent;
    }
}