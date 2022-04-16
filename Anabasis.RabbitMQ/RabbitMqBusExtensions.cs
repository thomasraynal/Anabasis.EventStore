using Anabasis.Common;
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
        public static void EmitRabbitMq<TEvent>(this IActor actor, IEnumerable<TEvent> events, string exchange, string exchangeType = "topic",
            TimeSpan? initialVisibilityDelay = null, 
            (string headerKey, string headerValue)[]? additionalHeaders = null)
              where TEvent : class, IRabbitMqEvent
        {
            var rabbitMqBus = actor.GetConnectedBus<IRabbitMqBus>();

            rabbitMqBus.Emit(events, exchange, exchangeType, initialVisibilityDelay, additionalHeaders: additionalHeaders);
        }

        public static void EmitRabbitMq<TEvent>(this IActor actor, TEvent @event, string exchange, string exchangeType= "topic", TimeSpan? initialVisibilityDelay = null, (string headerKey, string headerValue)[]? additionalHeaders = null)
                where TEvent : class, IRabbitMqEvent
        {
            var rabbitMqBus = actor.GetConnectedBus<IRabbitMqBus>();

            rabbitMqBus.Emit(@event, exchange, exchangeType, initialVisibilityDelay, additionalHeaders: additionalHeaders);
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
            string exchangeType = "topic",
            bool isAutoAck = false,
            bool isAutoDelete = false,
            Expression<Func<TEvent, bool>>? routingStrategy = null)
         where TEvent : class, IRabbitMqEvent
        {
            var rabbitMqBus = actor.GetConnectedBus<IRabbitMqBus>();

            var rabbitMqSubscription = new RabbitMqEventSubscription<TEvent>(exchange, exchangeType, (@event) =>
            {
                actor.OnMessageReceived(@event);

                return Task.CompletedTask;

            },actor: actor, isAutoDelete: isAutoDelete, isAutoAck: isAutoAck, routingStrategy: routingStrategy);

            rabbitMqBus.SubscribeToExchange(rabbitMqSubscription);

            var disposable = Disposable.Create(() => rabbitMqBus.UnsubscribeFromExchange(rabbitMqSubscription));

            actor.AddDisposable(disposable);
        }

        public static IObservable<TEvent> SubscribeToExchange<TEvent>(this IRabbitMqBus rabbitMqBus,
            string exchange,
            string exchangeType = "topic",
            bool isAutoAck = false,
            bool isAutoDelete = false,
            Expression<Func<TEvent, bool>>? routingStrategy = null)
        where TEvent : class, IRabbitMqEvent
        {

            var observable = Observable.Create<TEvent>((observer) =>
            {
                var rabbitMqSubscription = new RabbitMqEventSubscription<TEvent>(exchange, exchangeType, (@event) =>
                 {
                     observer.OnNext((TEvent)@event.Content);

                     return Task.CompletedTask;

                 }, isAutoDelete: isAutoDelete, isAutoAck: isAutoAck, routingStrategy: routingStrategy);

                rabbitMqBus.SubscribeToExchange(rabbitMqSubscription);

                return Disposable.Create(() => rabbitMqBus.UnsubscribeFromExchange(rabbitMqSubscription));
            });

            return observable;
        }
    }
}
