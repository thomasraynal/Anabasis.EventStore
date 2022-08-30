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
    public class SimpleQueueBufferTests
    {
        [Test]
        public void ShouldPullFromASimpleQueueBuffer()
        {
            var simpleQueueBuffer = new SimpleQueueBuffer(1, 0, 0);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsFalse(simpleQueueBuffer.CanDequeue());

            var testMessage = new TestEventBusMessage(EventA.New());

            simpleQueueBuffer.Push(testMessage);

            Assert.IsFalse(simpleQueueBuffer.CanPush);
            Assert.IsTrue(simpleQueueBuffer.CanDequeue());

            var messages = simpleQueueBuffer.Pull();

            Assert.IsNotEmpty(messages);

            Assert.AreEqual(testMessage.MessageId, messages[0].MessageId);


        }

        [Test]
        public async Task ShouldPullWhenReachingAbsoluteTimeout()
        {
            var simpleQueueBuffer = new SimpleQueueBuffer(10, 2, 0);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsFalse(simpleQueueBuffer.CanDequeue());

            var testMessage = new TestEventBusMessage(EventA.New());

            simpleQueueBuffer.Push(testMessage);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsTrue(simpleQueueBuffer.CanDequeue());

            var messages = simpleQueueBuffer.Pull();

            Assert.IsNotEmpty(messages);

            Assert.AreEqual(testMessage.MessageId, messages[0].MessageId);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsFalse(simpleQueueBuffer.CanDequeue());

            simpleQueueBuffer.Push(new TestEventBusMessage(EventA.New()));
            simpleQueueBuffer.Push(new TestEventBusMessage(EventA.New()));

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsFalse(simpleQueueBuffer.CanDequeue());

            await Task.Delay(1000);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsFalse(simpleQueueBuffer.CanDequeue());

            await Task.Delay(1000);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsTrue(simpleQueueBuffer.CanDequeue());

        }

        [Test]
        public void ShouldPullWhenReachingBufferMaxSize()
        {
            var simpleQueueBuffer = new SimpleQueueBuffer(2, 2, 0);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsFalse(simpleQueueBuffer.CanDequeue());

            var testMessage = new TestEventBusMessage(EventA.New());

            simpleQueueBuffer.Push(testMessage);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsTrue(simpleQueueBuffer.CanDequeue());

            var messages = simpleQueueBuffer.Pull();

            Assert.IsNotEmpty(messages);

            Assert.AreEqual(testMessage.MessageId, messages[0].MessageId);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsFalse(simpleQueueBuffer.CanDequeue());

            simpleQueueBuffer.Push(new TestEventBusMessage(EventA.New()));
            simpleQueueBuffer.Push(new TestEventBusMessage(EventA.New()));

            Assert.IsFalse(simpleQueueBuffer.CanPush);
            Assert.IsTrue(simpleQueueBuffer.CanDequeue());
        }

        [Test]
        public async Task ShouldDelayPullWhenSlidingInEffect()
        {
            var simpleQueueBuffer = new SimpleQueueBuffer(3, 2, 1);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsFalse(simpleQueueBuffer.CanDequeue());

            simpleQueueBuffer.Push(new TestEventBusMessage(EventA.New()));
            simpleQueueBuffer.Push(new TestEventBusMessage(EventA.New()));
            simpleQueueBuffer.Push(new TestEventBusMessage(EventA.New()));

            await Task.Delay(1000);

            var messages = simpleQueueBuffer.Pull();
            Assert.IsNotEmpty(messages);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsFalse(simpleQueueBuffer.CanDequeue());

            simpleQueueBuffer.Push(new TestEventBusMessage(EventA.New()));
     
            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsFalse(simpleQueueBuffer.CanDequeue());

            //buffer expire
            await Task.Delay(2000);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsTrue(simpleQueueBuffer.CanDequeue());

            simpleQueueBuffer.Push(new TestEventBusMessage(EventA.New()));

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsFalse(simpleQueueBuffer.CanDequeue());

            await Task.Delay(1000);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsTrue(simpleQueueBuffer.CanDequeue());

        }

    }
}
