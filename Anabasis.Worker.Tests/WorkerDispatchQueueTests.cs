using Anabasis.Common;
using Anabasis.Common.Worker;
using Anabasis.Worker.Tests.Model;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Worker.Tests
{
    [TestFixture]
    public class WorkerDispatchQueueTests
    {
        public class TesKillSwitch : IKillSwitch
        {
            public bool IsAppKilled { get; private set; }

            public void KillProcess(string reason)
            {
                IsAppKilled = true;
            }

            public void KillProcess(Exception exception)
            {
                IsAppKilled = true;
            }
        }

        [Theory]
        [TestCase(true)]
        [TestCase(false)]
        public async Task ShouldPushAndConsumeAndFail(bool shouldCrashAppOnError)
        {
            var rand = new Random();

            var bufferSize = 100;
            var messageBatchSize = bufferSize * 2;

            IMessage GetEvent()
            {
                IEvent @event = (rand.Next(0, 2) == 0 ? new EventA(Guid.NewGuid(), "EventA") : new EventB(Guid.NewGuid(), "EventB"));
                return new TestEventBusMessage(@event);
            }

            var receivedEvents = new List<IEvent>();
            var cancellationTokenSource = new CancellationTokenSource();

            var onEventsReceived = new Func<IEvent[], Task>((@events) =>
            {
                receivedEvents.AddRange(@events);

                throw new Exception("Boom!");

            });

            var workerDispatchQueue = new WorkerDispatchQueue("owner",
                new WorkerDispatchQueueConfiguration(onEventsReceived, shouldCrashAppOnError, bufferSize),
                cancellationTokenSource.Token, killSwitch: new TesKillSwitch());

            var messageBatch = Enumerable.Range(0, messageBatchSize).Select(_ => GetEvent()).ToArray();

            workerDispatchQueue.TryEnqueue(messageBatch, out var unprocessedMessages);

            await Task.Delay(200);

            Assert.AreEqual(shouldCrashAppOnError, workerDispatchQueue.IsFaulted);
            Assert.False(messageBatch.All((message) => (message as TestEventBusMessage).IsAcknowledge));

        }

        [Test]
        public async Task ShouldPushAndConsumeUpToTheBufferLimitAndSucceed()
        {

            var rand = new Random();

            var bufferSize = 100;
            var messageBatchSize = bufferSize * 2;

            IMessage GetEvent()
            {
                IEvent @event = (rand.Next(0, 2) == 0 ? new EventA(Guid.NewGuid(), "EventA") : new EventB(Guid.NewGuid(), "EventB"));
                return new TestEventBusMessage(@event);
            }

            var receivedEvents = new List<IEvent>();
            var cancellationTokenSource = new CancellationTokenSource();

            var onEventsReceived = new Func<IEvent[], Task>((@events) =>
            {
                receivedEvents.AddRange(@events);

                return Task.CompletedTask;

            });

            var workerDispatchQueue = new WorkerDispatchQueue("owner",
                new WorkerDispatchQueueConfiguration(onEventsReceived, false, bufferSize),
                cancellationTokenSource.Token);

            var messageBatch = Enumerable.Range(0, messageBatchSize).Select(_ => GetEvent()).ToArray();

            workerDispatchQueue.TryEnqueue(messageBatch, out var unprocessedMessages);

            await Task.Delay(200);

            Assert.AreEqual(messageBatchSize - bufferSize, unprocessedMessages.Length);
            Assert.AreEqual(messageBatchSize - bufferSize, receivedEvents.Count);

            workerDispatchQueue.TryEnqueue(unprocessedMessages, out  unprocessedMessages);

            await Task.Delay(200);

            Assert.AreEqual(0, unprocessedMessages.Length);
            Assert.AreEqual(messageBatchSize, receivedEvents.Count);

            Assert.True(messageBatch.All((message) => (message as TestEventBusMessage).IsAcknowledge));
        }
    }
}
