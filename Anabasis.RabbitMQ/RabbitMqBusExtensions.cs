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
              where TEvent : class, IRabbitMqMessage
        {
            var rabbitMqBus = actor.GetConnectedBus<IRabbitMqBus>();

            rabbitMqBus.Emit(events, exchange, initialVisibilityDelay);
        }

        public static void EmitRabbitMq<TEvent>(this IActor actor, TEvent @event, string exchange, TimeSpan? initialVisibilityDelay = null)
                where TEvent : class, IRabbitMqMessage
        {
            var rabbitMqBus = actor.GetConnectedBus<IRabbitMqBus>();

            rabbitMqBus.Emit(@event, exchange, initialVisibilityDelay);
        }

        public static TEvent[] PullRabbitMq<TEvent>(this IActor actor, string queueName, int? chunkSize = null)
                where TEvent : class, IRabbitMqMessage
        {
            var rabbitMqBus = actor.GetConnectedBus<IRabbitMqBus>();

            return rabbitMqBus.Pull(queueName, chunkSize)
                              .Cast<TEvent>()
                              .ToArray();
        }

        public static void SubscribeRabbitMq<TEvent>(this IActor actor, string exchange, Expression<Func<TEvent, bool>> routingStrategy = null)
            where TEvent : class, IRabbitMqMessage
        {
            var rabbitMqBus = actor.GetConnectedBus<IRabbitMqBus>();

            var rabbitMqSubscription = new RabbitMqEventSubscription<TEvent>(exchange, (@event) =>
            {
                return actor.OnEventReceived(@event);

            }, routingStrategy);

            rabbitMqBus.Subscribe(rabbitMqSubscription);

            var disposable = Disposable.Create(() => rabbitMqBus.Unsubscribe(rabbitMqSubscription));

            actor.AddDisposable(disposable);
        }

        public static IObservable<TEvent> SubscribeRabbitMq<TEvent>(this IRabbitMqBus rabbitMqBus, string exchange, Expression<Func<TEvent, bool>> routingStrategy = null)
        where TEvent : class, IRabbitMqMessage
        {

            var observable = Observable.Create<TEvent>((observer) =>
            {
                var rabbitMqSubscription = new RabbitMqEventSubscription<TEvent>(exchange, (@event) =>
                 {
                     observer.OnNext(@event);

                     return Task.CompletedTask;

                 }, routingStrategy);

                rabbitMqBus.Subscribe(rabbitMqSubscription);

                return Disposable.Create(() => rabbitMqBus.Unsubscribe(rabbitMqSubscription));
            });

            return observable;
        }
    }
}
