using Anabasis.Common;
using Anabasis.Common.Worker;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Worker.Tests
{
    [TestFixture]
    public class RoundRobinDispatcherStrategyTests
    {
        class TestWorkerDispatchQueue : IWorkerDispatchQueue
        {
            private readonly string _id = $"{Guid.NewGuid()}";

            public string Id => _id;

            public bool IsFaulted => false;

            public Exception LastError => throw new NotImplementedException();

            public string Owner => throw new NotImplementedException();

            public long ProcessedMessagesCount => throw new NotImplementedException();

            public bool CanPush()
            {
                return true;
            }

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public void Push(IMessage message)
            {
                throw new NotImplementedException();
            }

            public void TryPush(IMessage[] messages, out IMessage[] unProcessedMessages)
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public async Task ShouldPerformARoundRobinSelection()
        {
            var roundRobinDispatcherStrategy = new RoundRobinDispatcherStrategy();

            var testWorkerDispatchQueues = Enumerable.Range(0, 5).Select((_) =>
            {
                return new TestWorkerDispatchQueue();

            }).ToArray();

            roundRobinDispatcherStrategy.Initialize(testWorkerDispatchQueues);

            var retrievedDispatchedQueues = new List<IWorkerDispatchQueue>();

            for (var i = 0; i < testWorkerDispatchQueues.Length * 2; i++)
            {
                var (isDispatchQueueAvailable, workerDispatchQueue) = await roundRobinDispatcherStrategy.Next();

                Assert.IsTrue(isDispatchQueueAvailable);

                retrievedDispatchedQueues.Add(workerDispatchQueue);
            }

            var groups = retrievedDispatchedQueues.GroupBy(queue => queue.Id);

            await Task.Delay(1000);

            Assert.AreEqual(testWorkerDispatchQueues.Length * 2, retrievedDispatchedQueues.Count);

            foreach (var group in groups)
            {
                Assert.AreEqual(2, group.Count());
            }
        }
    }
}
