﻿using Anabasis.RabbitMQ;
using RabbitMQPlayground.Routing.Shared;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace RabbitMQPlayground.Routing
{

    public class RabbitMqEventSubscription<TEvent> : IRabbitMqEventSubscription<TEvent>
        where TEvent: class, IRabbitMqEvent
    {
        public string Exchange { get; }
        public string RoutingKey { get; protected set; }
        public string SubscriptionId { get; }
        public Func<TEvent,Task> OnEvent { get; }

        public RabbitMqEventSubscription(string exchange, Func<TEvent, Task> onEvent) : this(exchange, (_) => true, onEvent)
        {
       
        }

        public RabbitMqEventSubscription(string exchange, Expression<Func<TEvent, bool>> routingStrategy, Func<TEvent, Task> onEvent)
        {
            var rabbitMQSubjectExpressionVisitor = new RabbitMQSubjectExpressionVisitor(typeof(TEvent));

            rabbitMQSubjectExpressionVisitor.Visit(routingStrategy);

            OnEvent = onEvent;
            RoutingKey = rabbitMQSubjectExpressionVisitor.Resolve();
            Exchange = exchange;
            SubscriptionId = $"{Exchange}.{RoutingKey}";

        }

        public bool CanHandle(Type eventType)
        {
            return eventType == typeof(TEvent);
        }

        public Task Handle(IRabbitMqEvent rabbitMqEvent)
        {
            var eventType = rabbitMqEvent.GetType();

            if (!CanHandle(eventType))
                throw new InvalidOperationException($"{SubscriptionId} cannot handle event {eventType}");

            return OnEvent((TEvent)rabbitMqEvent);
        }
    }
}
