using Anabasis.Common;
using Anabasis.RabbitMQ.Event;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ
{
    public static class RabbitMqBusExtensions
    {
        public static void EmitRabbitMq<TEvent>(this IActor actor, IEnumerable<TEvent> events, string exchange, 
            string exchangeType = "topic",
            TimeSpan? initialVisibilityDelay = null,
            TimeSpan? messageExpiration = null,
            bool isMessagePersistent = true,
            bool isMessageMandatory = false,
            bool createExchangeIfNotExist = true,
            (string headerKey, string headerValue)[]? additionalHeaders = null)
              where TEvent : class, IRabbitMqEvent
        {
            var rabbitMqBus = actor.GetConnectedBus<IRabbitMqBus>();

            rabbitMqBus.Emit(events, exchange, exchangeType, initialVisibilityDelay, messageExpiration, isMessagePersistent, isMessageMandatory, createExchangeIfNotExist, additionalHeaders: additionalHeaders);
        }

        public static void EmitRabbitMq<TEvent>(this IActor actor, TEvent @event, string exchange, 
            string exchangeType = "topic", 
            TimeSpan? initialVisibilityDelay = null,
            TimeSpan? messageExpiration = null,
            bool isMessagePersistent = true,
            bool isMessageMandatory = false,
            bool createExchangeIfNotExist = true,
            (string headerKey, string headerValue)[]? additionalHeaders = null)
                where TEvent : class, IRabbitMqEvent
        {
            var rabbitMqBus = actor.GetConnectedBus<IRabbitMqBus>();

            rabbitMqBus.Emit(@event, exchange, exchangeType, initialVisibilityDelay, messageExpiration, isMessagePersistent, isMessageMandatory, createExchangeIfNotExist, additionalHeaders: additionalHeaders);
        }

        public static TEvent[] PullRabbitMq<TEvent>(this IActor actor, string queueName, bool isAutoAck = false, int? chunkSize = null)
                where TEvent : class, IRabbitMqEvent
        {
            var rabbitMqBus = actor.GetConnectedBus<IRabbitMqBus>();

            return rabbitMqBus.Pull(queueName, isAutoAck, chunkSize)
                              .Cast<TEvent>()
                              .ToArray();
        }

        public static void SubscribeToExchange<TEvent>(this IActor actor,
            string exchange,
            Expression<Func<TEvent, bool>>? routingStrategy = null,
            string queueName = "",
            string exchangeType = "topic",
            bool isExchangeDurable = true,
            bool isExchangeAutoDelete = false,
            bool createExchangeIfNotExist = true,
            bool isQueueDurable = false,
            bool isQueueAutoAck = false,
            bool isQueueAutoDelete = true,
            bool isQueueExclusive = true)
         where TEvent : class, IRabbitMqEvent
        {
            var rabbitMqBus = actor.GetConnectedBus<IRabbitMqBus>();

            var rabbitMqQueueConfiguration = new RabbitMqQueueConfiguration<TEvent>(routingStrategy,
                queueName,
                isQueueAutoAck,
                isQueueDurable,
                isQueueAutoDelete,
                isQueueExclusive);

            var rabbitMqExchangeConfiguration = new RabbitMqExchangeConfiguration(exchange,
                exchangeType,
                createExchangeIfNotExist,
                isExchangeAutoDelete,
                isExchangeDurable);

            var onMessage = new Func<IRabbitMqQueueMessage, Task>((@event) =>
            {
                actor.OnMessageReceived(@event);

                return Task.CompletedTask;

            });

            var rabbitMqSubscription = new RabbitMqEventSubscription<TEvent>(onMessage,
                rabbitMqQueueConfiguration,
                rabbitMqExchangeConfiguration);

            rabbitMqBus.SubscribeToExchange(rabbitMqSubscription);

            var disposable = Disposable.Create(() => rabbitMqBus.UnsubscribeFromExchange(rabbitMqSubscription));

            actor.AddToCleanup(disposable);
        }

        public static IObservable<TEvent> SubscribeToExchange<TEvent>(this IRabbitMqBus rabbitMqBus,
            string exchange,
            string queueName = "",
            string exchangeType = "topic",
            bool isExchangeDurable = true,
            bool isExchangeAutoDelete = false,
            bool isQueueDurable = false,
            bool isQueueAutoAck = false,
            bool isQueueAutoDelete = true,
            bool isQueueExclusive = true,
            Expression<Func<TEvent, bool>>? routingStrategy = null)
        where TEvent : class, IRabbitMqEvent
        {

            var rabbitMqQueueConfiguration = new RabbitMqQueueConfiguration<TEvent>(routingStrategy,
                queueName,
                isQueueAutoAck,
                isQueueDurable,
                isQueueAutoDelete,
                isQueueExclusive);

            var rabbitMqExchangeConfiguration = new RabbitMqExchangeConfiguration(exchange,
                exchangeType,
                true,
                isExchangeAutoDelete,
                isExchangeDurable);

            var observable = Observable.Create<TEvent>((observer) =>
            {
                var rabbitMqSubscription = new RabbitMqEventSubscription<TEvent>((@event) =>
                 {
                     observer.OnNext((TEvent)@event.Content);

                     return Task.CompletedTask;

                 }, rabbitMqQueueConfiguration, rabbitMqExchangeConfiguration);

                rabbitMqBus.SubscribeToExchange(rabbitMqSubscription);

                return Disposable.Create(() => rabbitMqBus.UnsubscribeFromExchange(rabbitMqSubscription));
            });

            return observable;
        }
    }
}
