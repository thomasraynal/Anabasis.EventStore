using System;
using System.Collections.Generic;
using System.Text;
using RabbitMQ.Client;

namespace RabbitMQPlayground.Routing
{
    public class BusConfiguration : IBusConfiguration
    {
        public BusConfiguration()
        {
            CommandTimeout = TimeSpan.FromSeconds(1);
        }

        public TimeSpan CommandTimeout { get; set; }
    }
}
