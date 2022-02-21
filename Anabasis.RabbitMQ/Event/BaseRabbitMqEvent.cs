﻿using Anabasis.Common;
using Anabasis.RabbitMQ;
using System;

namespace Anabasis.RabbitMQ.Event
{
    public class BaseRabbitMqEvent : IRabbitMqMessage
    {

        public BaseRabbitMqEvent(Guid? eventID, Guid? correlationId)
        {
            EventID = eventID ?? Guid.NewGuid();
            CorrelationID = correlationId ?? Guid.NewGuid();
        }

        public string Subject
        {
            get
            {
                return EventRoutingKey.GetRoutingKeyFromEvent(this);
            }
        }

        public Guid EventID { get; }

        public Guid CorrelationID { get; }

        public bool IsCommand => false;

        public string EntityId => Subject;

        public DateTime Timestamp { get; }

        public string Name => GetType().Name;
    }
}
