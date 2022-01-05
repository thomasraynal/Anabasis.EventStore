﻿using Anabasis.RabbitMQ;
using Anabasis.RabbitMQ.Routing.Bus;
using System;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ
{
    public interface IRabbitMqEventSubscription<TEvent> : IRabbitMqEventHandler where TEvent: IRabbitMqMessage
    {
        string SubscriptionId { get; }
        string Exchange { get; }
        string RoutingKey { get; }
        Func<TEvent, Task> OnEvent { get; }
    }
}