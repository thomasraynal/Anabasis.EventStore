using Anabasis.Common;
using Anabasis.RabbitMQ.Connection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Serilog;
using Serilog.Events;
using System;

namespace Anabasis.RabbitMQ.Tests.Integration
{
    [TestFixture]
    public class TestConnectionMonitor
    {
        [Test]
        public void ShouldConnectAndObserveConnection()
        {
            var rabbitMqConnectionOptions = new RabbitMqConnectionOptions()
            {
                HostName = "localhost",
                Password = "password",
                Username = "username",
            };


            Log.Logger = new LoggerConfiguration()
              .MinimumLevel.Information()
              .MinimumLevel.Override("Microsoft", LogEventLevel.Debug)
              .WriteTo.Debug()
              .CreateLogger();

            var loggerFactory = new LoggerFactory().AddSerilog(Log.Logger);

            var anabasisAppContext = new AnabasisAppContext("appName", "appGroup", new Version(1, 0));

            var connection = new RabbitMqConnection(rabbitMqConnectionOptions, anabasisAppContext, loggerFactory);

            var rabbitMqConnectionStatusMonitor = new RabbitMqConnectionStatusMonitor(connection, loggerFactory);

            Assert.Equals(true, rabbitMqConnectionStatusMonitor.IsConnected);
        }
    }
}
