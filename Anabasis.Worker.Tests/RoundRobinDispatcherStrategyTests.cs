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
            private string _id = $"{Guid.NewGuid()}";

            public string Id => _id;

            public bool IsFaulted => false;

            public Exception LastError => throw new NotImplementedException();

            public string Owner => throw new NotImplementedException();

            public bool CanEnqueue()
            {
                return true;
            }

            public ValueTask DisposeAsync()
            {
                throw new NotImplementedException();
            }

            public void Enqueue(IMessage message)
            {
                throw new NotImplementedException();
            }
        }

        [Test]
        public void ShouldPerformARoundRobinSelection()
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
                retrievedDispatchedQueues.Add(roundRobinDispatcherStrategy.Next());
            }

            var groups = retrievedDispatchedQueues.GroupBy(queue => queue.Id);

            Assert.AreEqual(testWorkerDispatchQueues.Length * 2, retrievedDispatchedQueues.Count);

            foreach(var group in groups)
            {
                Assert.AreEqual(2, group.Count());
            }
        }
    }
}
