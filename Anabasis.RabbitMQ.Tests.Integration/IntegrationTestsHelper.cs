using Anabasis.Api;
using Microsoft.Extensions.Logging;
using RabbitMQPlayground.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ.Tests.Integration
{
    public static class IntegrationTestsHelper
    {
        public static RabbitMqBus GetRabbitMqBus()
        {
            var rabbitMqConnectionOptions = new RabbitMqConnectionOptions()
            {
                HostName = "localhost",
                Password = "password",
                Username = "username",
            };
            var anabasisAppContext = new AnabasisAppContext("appName", "appGroup", new Version(1, 0));
            var loggerFactory = new LoggerFactory();
            var defaultSerializer = new JsonNetSerializer();

            var rabbitMqBus = new RabbitMqBus(
                rabbitMqConnectionOptions,
                anabasisAppContext,
                loggerFactory,
                defaultSerializer
                );

            return rabbitMqBus;
        }
    }
}
