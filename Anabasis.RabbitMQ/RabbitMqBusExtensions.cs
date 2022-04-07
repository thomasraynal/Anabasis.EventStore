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
        public static void EmitRabbitMq<TEvent>(this IActor actor, IEnumerable<TEvent> events, string exchange, TimeSpan? initialVisibilityDelay = null)
              where TEvent : class, IRabbitMqEvent
        {
            var rabbitMqBus = actor.GetConnectedBus<IRabbitMqBus>();

            rabbitMqBus.Emit(events, exchange, initialVisibilityDelay);
        }

        public static void EmitRabbitMq<TEvent>(this IActor actor, TEvent @event, string exchange, TimeSpan? initialVisibilityDelay = null)
                where TEvent : class, IRabbitMqEvent
        {
            var rabbitMqBus = actor.GetConnectedBus<IRabbitMqBus>();

            rabbitMqBus.Emit(@event, exchange, initialVisibilityDelay);
        }

        public static TEvent[] PullRabbitMq<TEvent>(this IActor actor, string queueName, bool isAutoAck = false, int? chunkSize = null)
                where TEvent : class, IRabbitMqEvent
        {
            var rabbitMqBus = actor.GetConnectedBus<IRabbitMqBus>();

            return rabbitMqBus.Pull(queueName, isAutoAck, chunkSize)
                              .Cast<TEvent>()
                              .ToArray();
        }

        public static void SubscribeToExchange<TEvent>(this IActor actor, string exchange, bool isAutoAck = true, Expression<Func<TEvent, bool>>? routingStrategy = null)
            where TEvent : class, IRabbitMqEvent
        {
            var rabbitMqBus = actor.GetConnectedBus<IRabbitMqBus>();

            var rabbitMqSubscription = new RabbitMqEventSubscription<TEvent>(exchange, (@event) =>
            {
                actor.OnMessageReceived(@event);

                return Task.CompletedTask;

            }, isAutoAck, routingStrategy);

            rabbitMqBus.Subscribe(rabbitMqSubscription);

            var disposable = Disposable.Create(() => rabbitMqBus.Unsubscribe(rabbitMqSubscription));

            actor.AddDisposable(disposable);
        }

        public static IObservable<TEvent> SubscribeToExchange<TEvent>(this IRabbitMqBus rabbitMqBus, string exchange, bool isAutoAck = true, Expression<Func<TEvent, bool>>? routingStrategy = null)
        where TEvent : class, IRabbitMqEvent
        {

            var observable = Observable.Create<TEvent>((observer) =>
            {
                var rabbitMqSubscription = new RabbitMqEventSubscription<TEvent>(exchange, (@event) =>
                 {
                     observer.OnNext((TEvent)@event.Content);

                     return Task.CompletedTask;

                 }, isAutoAck, routingStrategy);

                rabbitMqBus.Subscribe(rabbitMqSubscription);

                return Disposable.Create(() => rabbitMqBus.Unsubscribe(rabbitMqSubscription));
            });

            return observable;
        }
    }
}
