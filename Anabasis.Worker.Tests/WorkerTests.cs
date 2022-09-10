using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.Common.Contracts;
using Anabasis.Common.Worker;
using Anabasis.Worker.Tests.Model;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Worker.Tests
{



    [TestFixture]
    public class WorkerTests
    {
        [OneTimeSetUp]
        public void Setup()
        {

        }

        [Test]
        public async Task ShouldCreateAWorkerAndConsumeMessages()
        {
            var testWorker = new TestWorker(new WorkerConfiguration()
            {
                MessageBufferMaxSize = 50
            });

            var testEventBus = new TestEventBus();

            await testWorker.ConnectTo(testEventBus);

            testWorker.SubscribeToTestEventBus();

            await Task.Delay(2500);

            Assert.AreEqual(testEventBus.Messages.Count, testWorker.Events.Count);

            testEventBus.Dispose();
            testWorker.Dispose();

        }

        [Test]
        public async Task ShouldCreateAWorkerAndConsumeMessagesWithTwoDispatchQueues()
        {
            var testWorker = new TestWorker(new WorkerConfiguration()
            {
                DispatcherCount = 2,
                MessageBufferMaxSize = 10
            });

            var dispatchQueues = testWorker.GetWorkerDispatchQueues();
            Assert.AreEqual(2, dispatchQueues.Length);

            var testEventBus = new TestEventBus(500);

            await testWorker.ConnectTo(testEventBus);

            testWorker.SubscribeToTestEventBus();

            await Task.Delay(1500);

            foreach(var dispatchQueue in dispatchQueues)
            {
                Assert.Greater(dispatchQueue.ProcessedMessagesCount, 0);
            }

            testEventBus.Dispose();
            testWorker.Dispose();
        }

    }
}
