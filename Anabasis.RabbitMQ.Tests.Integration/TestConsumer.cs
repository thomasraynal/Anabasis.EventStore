using Anabasis.Common;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ.Tests.Integration
{
    public class Consumer
    {
        private readonly MessageHandlerInvokerCache _messageHandlerInvokerCache;
        private readonly RabbitMqBus _rabbitMqBus;

        public Consumer()
        {
            _messageHandlerInvokerCache = new MessageHandlerInvokerCache();

            _rabbitMqBus = IntegrationTestsHelper.GetRabbitMqBus();

            _rabbitMqBus.Subscribe(new RabbitMqEventSubscription<TestEventOne>("testevent-exchange", (ev) => ev.FilterOne == "filterOne", (ev) =>
            {
                var candidateHandler = _messageHandlerInvokerCache.GetMethodInfo(GetType(), typeof(TestEventOne));

                if (null != candidateHandler)
                {
                    ((Task)candidateHandler.Invoke(this, new object[] { ev })).Wait();
                }

                return Task.CompletedTask;

            }));
        }

        public Task Handle(TestEventOne testEventOne)
        {
            return Task.CompletedTask;
        }

        public void Emit()
        {
            _rabbitMqBus.Emit(new TestEventOne(Guid.NewGuid(), Guid.NewGuid()) { FilterOne = "filterOne" }, "testevent-exchange");

        }

    }


  
    public class TestConsumer
    {
        [Test]
        public async Task Test()
        {
            var consumer = new Consumer();
            consumer.Emit();

            await Task.Delay(1000);
        }
    }
}
