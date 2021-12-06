using NUnit.Framework;
using RabbitMQPlayground.Routing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ.Tests.Integration
{
    [TestFixture]
    public class IntegrationTestsSubscribe
    {
        private RabbitMqBus _rabbitMqBus;
        private int _counter;

        [OneTimeSetUp]
        public void SetUp()
        {
            _rabbitMqBus = IntegrationTestsHelper.GetRabbitMqBus();
            _counter = 0;
        }

        [Test, Order(1)]
        public async Task ShouldCreateSomeSubscriptions()
        {

            _rabbitMqBus.Subscribe(new RabbitMqEventSubscription<TestEventOne>("testevent-exchange",(ev)=>
            {
                _counter++;
                return Task.CompletedTask;
            }));

            _rabbitMqBus.Subscribe(new RabbitMqEventSubscription<TestEventOne>("testevent-exchange", (ev) => ev.Data == "one", (ev) =>
            {
                _counter++;
                return Task.CompletedTask;
            }));

            _rabbitMqBus.Subscribe(new RabbitMqEventSubscription<TestEventTwo>("testevent-exchange", (ev) => ev.Data2 == "data2", (ev) =>
            {
                _counter++;
                return Task.CompletedTask;
            }));
        }

        [Test, Order(2)]
        public async Task ShouldEmitEventAndConsumeIt()
        {
            _rabbitMqBus.Emit(new TestEventOne(Guid.NewGuid(), Guid.NewGuid()) { Data = "one" }, "testevent-exchange");

            await Task.Delay(500);

            Assert.AreEqual(2, _counter);

            _rabbitMqBus.Emit(new TestEventTwo(Guid.NewGuid(), Guid.NewGuid()) { Data = "one" }, "testevent-exchange");

            await Task.Delay(500);

            Assert.AreEqual(2, _counter);

            _rabbitMqBus.Emit(new TestEventTwo(Guid.NewGuid(), Guid.NewGuid()) { Data2 = "data2" }, "testevent-exchange");

            await Task.Delay(500);

            Assert.AreEqual(3, _counter);
        }
    }
}
