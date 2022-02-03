using Anabasis.Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Tests
{
    [TestFixture]
    public class TestDispatchQueue
    {
      //  [TestCase(6, 6, 6, 0)]
        [TestCase(1, 3, 3, 100)]
        public async Task ShouldCreateADispatchQueueAndEnqueueThreeMessage(int batchSize, int queueMaxSize, int messageCount, int messageConsumptionWait)
        {
            var messages = new List<int>();

            var dispatchQueue = new DispatchQueue<int>(async (m) =>
            {
                messages.Add(m);
                await Task.Delay(messageConsumptionWait);

            }, batchSize, queueMaxSize);

            await Task.Delay(100);

            for (var i = 0; i < messageCount; i++)
            {
                dispatchQueue.Enqueue(i);
            }

            await Task.Delay(messageConsumptionWait / 2);

            Assert.AreEqual(batchSize, messages.Count);

            dispatchQueue.Dispose();

            Assert.AreEqual(messageCount, messages.Count);
        }



    }
}
