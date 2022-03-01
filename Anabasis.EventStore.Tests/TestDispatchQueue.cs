using Anabasis.Common;
using Anabasis.Common.Queue;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Tests
{
    public class TestEvent : IEvent
    {
        public TestEvent(int value)
        {
            Value = value;
        }

        public int Value { get; }
        public Guid EventId => Guid.NewGuid();

        public Guid CorrelationId => Guid.NewGuid();

        public string Name => nameof(TestEvent);

        public bool IsCommand => false;

        public DateTime Timestamp => DateTime.UtcNow;

        public string EntityId => nameof(TestEvent);
    }


    public class TestMessage : IMessage
    {
        public int Value { get; }

        public int DequeueCount => 0;

        public Guid MessageId => Guid.NewGuid();

        public IEvent Content { get; }

        public TestMessage(int value)
        {
            Content = new TestEvent(value);
        }

        public Task Acknowledge()
        {
            return Task.CompletedTask;
        }

        public Task NotAcknowledge(string reason = null)
        {
            return Task.CompletedTask;
        }
    }

    [TestFixture]
    public class TestDispatchQueue
    {
        [Ignore("tofix")]
        [TestCase(6, 12, 12, 100)]
        [TestCase(1, 3, 3, 100)]
        public async Task ShouldCreateADispatchQueueAndEnqueueMessagesThenDispose(int batchSize, int queueMaxSize, int messageCount, int messageConsumptionWait)
        {
            
            var messages = new List<IEvent>();

            var dispatchQueueConfiguration = new DispatchQueueConfiguration(
                async (m) =>
                {

                    messages.Add(m);
                    await Task.Delay(messageConsumptionWait + 5);

                },
                batchSize,
                queueMaxSize
                );

            var dispatchQueue = new DispatchQueue(dispatchQueueConfiguration, new DummyLoggerFactory());

            Assert.True(dispatchQueue.CanEnqueue());

            await Task.Delay(100);

            for (var i = 0; i < messageCount; i++)
            {
                dispatchQueue.Enqueue(new TestMessage(i));
            }

            await Task.Delay(messageConsumptionWait * batchSize);

            Assert.AreEqual(batchSize, messages.Count);

            dispatchQueue.Dispose();

            Assert.IsFalse(dispatchQueue.CanEnqueue());

            Assert.AreEqual(messageCount, messages.Count);
        }

        [Test]
        public void ShouldTryToEnqueueAndFail()
        {

            var dispatchQueueConfiguration = new DispatchQueueConfiguration(
                async (m) =>
                {
                    await Task.Delay(10);

                },
                10,
                10
                );

            var dispatchQueue = new DispatchQueue(dispatchQueueConfiguration, new DummyLoggerFactory());

            dispatchQueue.Dispose();

            Assert.IsFalse(dispatchQueue.CanEnqueue());

            Assert.Throws<InvalidOperationException>(() => dispatchQueue.Enqueue(new TestMessage(1)));

        }

    }
}
