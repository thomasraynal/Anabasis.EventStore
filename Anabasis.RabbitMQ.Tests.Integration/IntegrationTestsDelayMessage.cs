﻿using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ.Tests.Integration
{
    [TestFixture]
    public class IntegrationTestsDelayMessage
    {
        private RabbitMqBus _rabbitMqBus;
        private int _counter;

        [OneTimeSetUp]
        public void SetUp()
        {
            _rabbitMqBus = IntegrationTestsHelper.GetRabbitMqBus();
        }

        [Test, Order(1)]
        public void ShouldCreateSusbscription()
        {
            _rabbitMqBus.SubscribeToExchange(new RabbitMqEventSubscription<TestEventOne>("testevent-exchange", "topic",
                (ev) =>
            {
                _counter++;
                return Task.CompletedTask;
            }, 
            isExchangeDurable : false,
            isExchangeAutoDelete : true,
            isQueueDurable: false,
            isQueueAutoAck: false,
            isQueueAutoDelete: true,
            isQueueExclusive: true));
        }

        [Test, Order(2)]
        public async Task ShouldSubmitMessageAndDelayIt()
        {
            _rabbitMqBus.Emit(new TestEventOne(Guid.NewGuid(), Guid.NewGuid()) { FilterOne = "AnotherfilterOne" }, "testevent-exchange", initialVisibilityDelay:  TimeSpan.FromSeconds(3), isMessagePersistent: false);

            await Task.Delay(1000);

            Assert.AreEqual(0, _counter);

            await Task.Delay(2500);

            Assert.AreEqual(1, _counter);

        }

    }
}
