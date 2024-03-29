﻿using Newtonsoft.Json;
using System;

namespace Anabasis.RabbitMQ.Event
{
    public class BaseRabbitMqEvent : IRabbitMqEvent
    {
        [JsonConstructor]
        private BaseRabbitMqEvent() { }

        public BaseRabbitMqEvent(Guid? messageId, Guid? eventId, Guid? correlationId, Guid? causeId = null, Guid? traceId = null)
        {
            MessageId = messageId ?? Guid.NewGuid();
            EventId = eventId ?? Guid.NewGuid();
            CorrelationId = correlationId ?? Guid.NewGuid();
            CauseId = causeId;
            TraceId = traceId;
        }

        public virtual string Subject
        {
            get
            {
                return EventRoutingKey.GetRoutingKeyFromEvent(this);
            }
        }

        public ulong DeliveryTag { get; }

        public Guid EventId { get; }

        public Guid CorrelationId { get; }

        public bool IsCommand => false;

        public string EntityId => Subject;

        public DateTime Timestamp { get; }

        public string EventName => GetType().Name;

        public Guid MessageId { get; }

        public Guid? CauseId { get; }

        public bool IsAggregateEvent => false;

        public Guid? TraceId { get; }
    }
}
