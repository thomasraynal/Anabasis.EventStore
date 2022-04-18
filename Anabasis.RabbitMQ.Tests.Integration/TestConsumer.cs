using Anabasis.Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ.Tests.Integration
{
    public class Consumer
    {
        private readonly MessageHandlerInvokerCache _messageHandlerInvokerCache;
        private readonly RabbitMqBus _rabbitMqBus;

        public List<TestEventOne> Events { get; set; } = new List<TestEventOne>();

        public Consumer()
        {
            _messageHandlerInvokerCache = new MessageHandlerInvokerCache();

            _rabbitMqBus = IntegrationTestsHelper.GetRabbitMqBus();

            _rabbitMqBus.SubscribeToExchange(new RabbitMqEventSubscription<TestEventOne>("testevent-exchange", "topic", onMessage: (ev) =>
            {
                var candidateHandler = _messageHandlerInvokerCache.GetMethodInfo(GetType(), typeof(TestEventOne));

                if (null != candidateHandler)
                {
                    ((Task)candidateHandler.Invoke(this, new object[] { ev.Content })).Wait();
                }

                return Task.CompletedTask;

            }, 
            isExchangeDurable: false,
            isExchangeAutoDelete: true,
            isQueueDurable: false,
            isQueueAutoAck: false,
            isQueueAutoDelete: true,
            isQueueExclusive: true,
            routingStrategy: (ev) => ev.FilterOne == "filterOne"));
        }

        public Task Handle(TestEventOne testEventOne)
        {
            Events.Add(testEventOne);

            return Task.CompletedTask;
        }

        public void Emit()
        {
            _rabbitMqBus.Emit(new TestEventOne(Guid.NewGuid(), Guid.NewGuid()) { FilterOne = "filterOne" }, "testevent-exchange",
                isMessageMandatory: false,
                isMessagePersistent: false);
        }

    }


    public class TestConsumer
    {
        [Test]
        public async Task TestBaseLine()
        {
            var consumer = new Consumer();
            consumer.Emit();

            await Task.Delay(500);

            Assert.IsTrue(consumer.Events.Count > 0);
        }
    }
}
