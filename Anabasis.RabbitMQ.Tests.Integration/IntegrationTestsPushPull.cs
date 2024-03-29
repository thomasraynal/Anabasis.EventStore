﻿using NUnit.Framework;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ.Tests.Integration
{
    [TestFixture]
    public class IntegrationTestsPushPull
    {

        [Test, Order(1)]
        public async Task ShouldCreateAQueueAndPushMessageAndPullMessage()
        {

            var rabbitMqBus = IntegrationTestsHelper.GetRabbitMqBus();

            var testExchangeName = $"test-exchange-{Guid.NewGuid()}";
            var testQueueName = $"test-queue-{Guid.NewGuid()}";

            var testQueue = rabbitMqBus.RabbitMqConnection.DoWithChannel((channel) =>
                channel.QueueDeclare(testQueueName, exclusive: true, autoDelete: true).QueueName);

            rabbitMqBus.RabbitMqConnection.DoWithChannel((channel) =>
                channel.ExchangeDeclare(testExchangeName, type: "x-delayed-message", durable: false, autoDelete: true, new Dictionary<string, object>()
                    {
                        {"x-delayed-type","topic"}
                    }));

            rabbitMqBus.RabbitMqConnection.DoWithChannel((channel) =>
                channel.QueueBind(testQueueName, testExchangeName, "TestEventOne.#"));

            rabbitMqBus.Emit(new TestEventOne(Guid.NewGuid(), Guid.NewGuid()), 
                testExchangeName, 
                isMessagePersistent: false);

            await Task.Delay(100);

            var events =  rabbitMqBus.Pull(testQueueName,true, 1);

            Assert.AreEqual(1, events.Length);
        }

    }
}
