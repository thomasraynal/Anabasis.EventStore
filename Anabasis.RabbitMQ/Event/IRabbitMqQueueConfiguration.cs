using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.RabbitMQ.Event
{
    public interface IRabbitMqQueueConfiguration
    {
        string? QueueName { get; }
        string RoutingKey { get; }
        bool IsAutoAck { get; }
        bool IsDurable { get; }
        bool IsAutoDelete { get; }
        bool IsExclusive { get; }
    }
}
