using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ.Tests.Integration
{
    [TestFixture]
    public class IntegrationTestsSubscribe
    {
        private RabbitMqBus _rabbitMqBus;
        private int _counterTestEventOneNoFilter;
        private int _counterTestEventTwoNoFilter;
        private int _counterTestEventOneFilterOnFilterOne;
        private int _counterTestEventTwoFilterOnFilterTwo;

        [OneTimeSetUp]
        public void SetUp()
        {
            _rabbitMqBus = IntegrationTestsHelper.GetRabbitMqBus();

            _counterTestEventOneNoFilter = 0;
            _counterTestEventTwoNoFilter = 0;

            _counterTestEventOneFilterOnFilterOne = 0;
            _counterTestEventTwoFilterOnFilterTwo = 0;
        }

        [Test, Order(1)]
        public void ShouldCreateSomeSubscriptions()
        {

            _rabbitMqBus.Subscribe(new RabbitMqEventSubscription<TestEventOne>("testevent-exchange", (ev) =>
             {
                 _counterTestEventOneNoFilter++;
                 return Task.CompletedTask;
             }));

            _rabbitMqBus.Subscribe(new RabbitMqEventSubscription<TestEventOne>("testevent-exchange", (ev) =>
            {
                _counterTestEventOneFilterOnFilterOne++;
                return Task.CompletedTask;

            }, true, (ev) => ev.FilterOne == "filterOne"));

            _rabbitMqBus.Subscribe(new RabbitMqEventSubscription<TestEventTwo>("testevent-exchange", (ev) =>
            {
                _counterTestEventTwoNoFilter++;
                return Task.CompletedTask;

            }));

            _rabbitMqBus.Subscribe(new RabbitMqEventSubscription<TestEventTwo>("testevent-exchange", (ev) =>
            {
                _counterTestEventTwoFilterOnFilterTwo++;
                return Task.CompletedTask;

            }, true, (ev) => ev.FilterTwo == "filterTwo"));
        }

        [Test, Order(2)]
        public async Task ShouldEmitEventsAndConsumeThem()
        {
            _rabbitMqBus.Emit(new TestEventOne(Guid.NewGuid(), Guid.NewGuid()) { FilterOne = "AnotherfilterOne" }, "testevent-exchange");

            await Task.Delay(500);

            Assert.AreEqual(0, _counterTestEventTwoNoFilter);
            Assert.AreEqual(0, _counterTestEventTwoFilterOnFilterTwo);
            Assert.AreEqual(1, _counterTestEventOneNoFilter);
            Assert.AreEqual(0, _counterTestEventOneFilterOnFilterOne);
    
            _rabbitMqBus.Emit(new TestEventOne(Guid.NewGuid(), Guid.NewGuid()) { FilterOne = "filterOne" }, "testevent-exchange");

            await Task.Delay(500);

            Assert.AreEqual(0, _counterTestEventTwoNoFilter);
            Assert.AreEqual(0, _counterTestEventTwoFilterOnFilterTwo);
            Assert.AreEqual(2, _counterTestEventOneNoFilter);
            Assert.AreEqual(1, _counterTestEventOneFilterOnFilterOne);

            _rabbitMqBus.Emit(new TestEventTwo(Guid.NewGuid(), Guid.NewGuid()) { FilterOne = "filterOne" }, "testevent-exchange");

            await Task.Delay(500);

            Assert.AreEqual(1, _counterTestEventTwoNoFilter);
            Assert.AreEqual(0, _counterTestEventTwoFilterOnFilterTwo);
            Assert.AreEqual(2, _counterTestEventOneNoFilter);
            Assert.AreEqual(1, _counterTestEventOneFilterOnFilterOne);

            _rabbitMqBus.Emit(new TestEventTwo(Guid.NewGuid(), Guid.NewGuid()) { FilterTwo = "filterTwo" }, "testevent-exchange");

            await Task.Delay(500);

            Assert.AreEqual(2, _counterTestEventTwoNoFilter);
            Assert.AreEqual(1, _counterTestEventTwoFilterOnFilterTwo);
            Assert.AreEqual(2, _counterTestEventOneNoFilter);
            Assert.AreEqual(1, _counterTestEventOneFilterOnFilterOne);
        }
    }
}
