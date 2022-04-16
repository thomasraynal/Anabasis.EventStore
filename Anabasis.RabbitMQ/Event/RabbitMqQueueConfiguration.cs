﻿using Anabasis.RabbitMQ.Shared;
using System;
using System.Linq.Expressions;

namespace Anabasis.RabbitMQ.Event
{
    public class RabbitMqQueueConfiguration<TEvent> : IRabbitMqQueueConfiguration
         where TEvent : class, IRabbitMqEvent
    {
        public RabbitMqQueueConfiguration(Expression<Func<TEvent, bool>>? routingStrategy = null,
            string? queueName = null,
            bool isAutoAck = false,
            bool isDurable = true,
            bool isAutoDelete = false,
            bool isExclusive = false)
        {

            if (null == routingStrategy)
                routingStrategy = (_) => true;

            var rabbitMQSubjectExpressionVisitor = new RabbitMQSubjectExpressionVisitor(typeof(TEvent));
            rabbitMQSubjectExpressionVisitor.Visit(routingStrategy);

            RoutingKey = rabbitMQSubjectExpressionVisitor.Resolve();
            QueueName = queueName;
            IsAutoAck = isAutoAck;
            IsDurable = isDurable;
            IsAutoDelete = isAutoDelete;
            IsExclusive = isExclusive;
        }

        public string? QueueName { get; }

        public string RoutingKey { get; }

        public bool IsAutoAck { get; }

        public bool IsDurable { get; }

        public bool IsAutoDelete { get; }

        public bool IsExclusive { get; }
    }
}