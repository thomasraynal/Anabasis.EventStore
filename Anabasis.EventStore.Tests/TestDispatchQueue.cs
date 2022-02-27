﻿using Anabasis.Common;
using Anabasis.Common.Queue;
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
        [Ignore("tofix")]
        [TestCase(6, 12, 12, 100)]
        [TestCase(1, 3, 3, 100)]
        public async Task ShouldCreateADispatchQueueAndEnqueueMessagesThenDispose(int batchSize, int queueMaxSize, int messageCount, int messageConsumptionWait)
        {
            
            var messages = new List<int>();

            var dispatchQueueConfiguration = new DispatchQueueConfiguration<int>(
                async (m) =>
                {

                    messages.Add(m);
                    await Task.Delay(messageConsumptionWait + 5);

                },
                batchSize,
                queueMaxSize
                );

            var dispatchQueue = new DispatchQueue<int>(dispatchQueueConfiguration);

            Assert.True(dispatchQueue.CanEnqueue());

            await Task.Delay(100);

            for (var i = 0; i < messageCount; i++)
            {
                dispatchQueue.Enqueue(i);
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

            var dispatchQueueConfiguration = new DispatchQueueConfiguration<int>(
                async (m) =>
                {
                    await Task.Delay(10);

                },
                10,
                10
                );

            var dispatchQueue = new DispatchQueue<int>(dispatchQueueConfiguration);

            dispatchQueue.Dispose();

            Assert.IsFalse(dispatchQueue.CanEnqueue());

            Assert.Throws<InvalidOperationException>(() => dispatchQueue.Enqueue(1));

        }

    }
}
