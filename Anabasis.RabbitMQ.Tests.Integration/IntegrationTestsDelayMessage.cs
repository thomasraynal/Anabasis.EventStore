using NUnit.Framework;
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
            _rabbitMqBus.Subscribe(new RabbitMqEventSubscription<TestEventOne>("testevent-exchange", (ev) =>
            {
                _counter++;
                return Task.CompletedTask;
            }));
        }

        [Test, Order(2)]
        public async Task ShouldSubmitMessageAndDelayIt()
        {
            _rabbitMqBus.Emit(new TestEventOne(Guid.NewGuid(), Guid.NewGuid()) { FilterOne = "AnotherfilterOne" }, "testevent-exchange", TimeSpan.FromSeconds(3));

            await Task.Delay(1000);

            Assert.AreEqual(0, _counter);

            await Task.Delay(2500);

            Assert.AreEqual(1, _counter);

        }

    }
}
