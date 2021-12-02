using Anabasis.Api;
using Anabasis.EventStore.Serialization;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Polly.Retry;
using System;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ.Tests.Integration
{
    [TestFixture, Category("Integration")]
    public class Integration
    {
        class TestEvent : IEvent
        {
            public TestEvent()
            {
                EventID = CorrelationID = Guid.NewGuid();
            }

            public Guid EventID { get; }

            public Guid CorrelationID { get; }
        }


        [OneTimeSetUp]
        public void SetUp()
        {
        
        }

        [Test, Order(1)]
        public async Task ShouldCreateAQueueAndPushMessage()
        {
            var rabbitMqConnectionOptions = new RabbitMqConnectionOptions()
            {
                HostName = "localhost",
                Password = "password",
                Username = "username",
            };
            var anabasisAppContext = new AnabasisAppContext("appName", "appGroup", new Version(1, 0));
            var loggerFactory = new LoggerFactory();
            var serializer = new DefaultSerializer();

            var rabbitMqQueue = new RabbitMqQueue("queueName", 
                rabbitMqConnectionOptions,
                anabasisAppContext,
                serializer,
                loggerFactory
                );

            var testEvent = new TestEvent();

            rabbitMqQueue.Push(testEvent);
        }

    }
}
