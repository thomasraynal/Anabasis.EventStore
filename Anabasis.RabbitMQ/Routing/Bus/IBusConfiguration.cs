using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System;

namespace RabbitMQPlayground.Routing
{
    public interface IBusConfiguration
    {
        TimeSpan CommandTimeout { get; set; }
    }
}