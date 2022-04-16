using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.RabbitMQ.Shared
{
    public static class RabbitExtensions
    {
        //https://github.com/spring-attic/spring-net-amqp/blob/f019aa1d0577aea38de02ef320ec9dcc33d0d00f/src/Spring.Messaging.Amqp.Rabbit/Support/DateExtensions.cs
        public static AmqpTimestamp ToAmqpTimestamp(this DateTime datetime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var unixTime = (datetime.ToUniversalTime() - epoch).TotalSeconds;
            var timestamp = new AmqpTimestamp((long)unixTime);
            return timestamp;
        }
    }
}
