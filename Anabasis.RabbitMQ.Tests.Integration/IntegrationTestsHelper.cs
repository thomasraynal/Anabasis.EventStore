using Anabasis.Common;
using Microsoft.Extensions.Logging;
using System;

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
            var defaultSerializer = new DefaultSerializer();

            var rabbitMqBus = new RabbitMqBus(
                rabbitMqConnectionOptions,
                anabasisAppContext,
                defaultSerializer,
                loggerFactory
                );

            return rabbitMqBus;
        }
    }
}
