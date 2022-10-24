using Anabasis.Common;
using Anabasis.Common.Queue;
using NSubstitute;
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

        public string EventName => nameof(TestEvent);

        public bool IsCommand => false;

        public DateTime Timestamp => DateTime.UtcNow;

        public string EntityId => nameof(TestEvent);

        public Guid? CauseId => null;

        public bool IsAggregateEvent => false;

        public Guid? TraceId { get; set; }
    }


    public class TestMessage : IMessage
    {
        public int Value { get; }
        public bool IsAcknowledge { get; set; }
        public int DequeueCount => 0;

        public Guid MessageId => Guid.NewGuid();

        public IEvent Content { get; }

        public Guid? TraceId { get; set; }

        public TestMessage(int value)
        {
            Content = new TestEvent(value);
        }

        public Task Acknowledge()
        {
            IsAcknowledge = true;
            return Task.CompletedTask;
        }

        public Task NotAcknowledge(string reason = null)
        {
            IsAcknowledge = false;
            return Task.CompletedTask;
        }
    }

    [TestFixture]
    public class TestDispatchQueue
    {
        [Ignore("went non deterministic when switched to thread")]
        // [TestCase(6, 12, 12, 100)]
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

            var dispatchQueue = new DispatchQueue("void", dispatchQueueConfiguration, new DummyLoggerFactory());

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

            var dispatchQueue = new DispatchQueue("void", dispatchQueueConfiguration, new DummyLoggerFactory());

            dispatchQueue.Dispose();

            Assert.IsFalse(dispatchQueue.CanEnqueue());

            Assert.Throws<InvalidOperationException>(() => dispatchQueue.Enqueue(new TestMessage(1)));

        }

        [TestCase(true)]
        [TestCase(false)]
        public async Task ShouldTryToEnqueueAndKillApp(bool shouldCrashApp)
        {
            var killSwitch = Substitute.For<IKillSwitch>();

            var message1 = new TestMessage(0);
            var message2 = new TestMessage(1);

            var dispatchQueueConfiguration = new DispatchQueueConfiguration(
                 (m) =>
                {
                    if ((m as TestEvent).Value == message1.Value)
                        throw new Exception("boom");

                    return Task.CompletedTask;
                },
                10,
                10,
                shouldCrashApp);

            var dispatchQueue = new DispatchQueue("void", dispatchQueueConfiguration, new DummyLoggerFactory(), killSwitch);

            dispatchQueue.Enqueue(message1);
            dispatchQueue.Enqueue(message2);

            await Task.Delay(100);

            killSwitch.Received(shouldCrashApp ? 1 : 0).KillProcess(Arg.Any<Exception>());

            if (shouldCrashApp)
            {
                Assert.IsFalse(message1.IsAcknowledge);
                Assert.IsFalse(message2.IsAcknowledge);
                Assert.IsFalse(dispatchQueue.CanEnqueue());
                Assert.IsTrue(dispatchQueue.IsFaulted);
            }
            else
            {
                Assert.IsFalse(message1.IsAcknowledge);
                Assert.IsTrue(message2.IsAcknowledge);
                Assert.IsTrue(dispatchQueue.CanEnqueue());
                Assert.IsFalse(dispatchQueue.IsFaulted);
            }
        }

        [Test]
        public async Task ShouldNackAllMessagesAfterACriticalFailure()
        {
            var killSwitch = Substitute.For<IKillSwitch>();

            var dispatchQueueConfiguration = new DispatchQueueConfiguration(
                 (m) =>
                 {
                     if ((m as TestEvent).Value == 1)
                         throw new Exception("boom");

                     return Task.CompletedTask;
                 },
                10,
                10,
                true);


            var dispatchQueue = new DispatchQueue("void", dispatchQueueConfiguration, new DummyLoggerFactory(), killSwitch);

            var messages = new List<TestMessage>();
            var i = 0;

            while (dispatchQueue.CanEnqueue() && messages.Count < 100)
            {
                var message = new TestMessage(i++);
                messages.Add(message);
                dispatchQueue.Enqueue(message);
            }

            await Task.Delay(100);

            Assert.IsFalse(dispatchQueue.CanEnqueue());
            Assert.IsTrue(dispatchQueue.IsFaulted);

            Assert.IsTrue(messages.First().IsAcknowledge);

            foreach (var message in messages.Skip(1))
            {
                Assert.IsFalse(message.IsAcknowledge);
            }


        }


        [Test]
        public async Task ShouldResumeMessageProcessingAfterFailure()
        {
            var killSwitch = Substitute.For<IKillSwitch>();

            var dispatchQueueConfiguration = new DispatchQueueConfiguration(
                 (m) =>
                 {
                     if ((m as TestEvent).Value == 2)
                         throw new Exception("boom");

                     return Task.CompletedTask;
                 },
                10,
                10,
                false);


            var dispatchQueue = new DispatchQueue("void", dispatchQueueConfiguration, new DummyLoggerFactory(), killSwitch);

            var messages = new List<TestMessage>();
            var i = 0;

            while (dispatchQueue.CanEnqueue() && messages.Count < 100)
            {
                var message = new TestMessage(i++);
                messages.Add(message);
                dispatchQueue.Enqueue(message);
            }

            await Task.Delay(100);

            Assert.IsTrue(dispatchQueue.CanEnqueue());
            Assert.IsFalse(dispatchQueue.IsFaulted);

            Assert.IsTrue(messages.Where(m => !m.IsAcknowledge).Count() == 1);
        }
    }
}
