using Anabasis.Common.Worker;
using Anabasis.Worker.Tests.Model;
using NUnit.Framework;
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
            Assert.IsFalse(simpleQueueBuffer.CanPull);

            var testMessage = new TestEventBusMessage(EventA.New());

            simpleQueueBuffer.Push(testMessage);

            Assert.IsFalse(simpleQueueBuffer.CanPush);
            Assert.IsTrue(simpleQueueBuffer.CanPull);

            var messages = simpleQueueBuffer.Pull();

            Assert.IsNotEmpty(messages);

            Assert.AreEqual(testMessage.MessageId, messages[0].MessageId);


        }

        [Test]
        public async Task ShouldPullWhenReachingAbsoluteTimeout()
        {
            var simpleQueueBuffer = new SimpleQueueBuffer(10, 2, 0);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsFalse(simpleQueueBuffer.CanPull);

            var testMessage = new TestEventBusMessage(EventA.New());

            simpleQueueBuffer.Push(testMessage);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsTrue(simpleQueueBuffer.CanPull);

            var messages = simpleQueueBuffer.Pull();

            Assert.IsNotEmpty(messages);

            Assert.AreEqual(testMessage.MessageId, messages[0].MessageId);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsFalse(simpleQueueBuffer.CanPull);

            simpleQueueBuffer.Push(new TestEventBusMessage(EventA.New()));
            simpleQueueBuffer.Push(new TestEventBusMessage(EventA.New()));

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsFalse(simpleQueueBuffer.CanPull);

            await Task.Delay(1000);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsFalse(simpleQueueBuffer.CanPull);

            await Task.Delay(1000);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsTrue(simpleQueueBuffer.CanPull);

        }

        [Test]
        public void ShouldPullWhenReachingBufferMaxSize()
        {
            var simpleQueueBuffer = new SimpleQueueBuffer(2, 2, 0);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsFalse(simpleQueueBuffer.CanPull);

            var testMessage = new TestEventBusMessage(EventA.New());

            simpleQueueBuffer.Push(testMessage);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsTrue(simpleQueueBuffer.CanPull);

            var messages = simpleQueueBuffer.Pull();

            Assert.IsNotEmpty(messages);

            Assert.AreEqual(testMessage.MessageId, messages[0].MessageId);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsFalse(simpleQueueBuffer.CanPull);

            simpleQueueBuffer.Push(new TestEventBusMessage(EventA.New()));
            simpleQueueBuffer.Push(new TestEventBusMessage(EventA.New()));

            Assert.IsFalse(simpleQueueBuffer.CanPush);
            Assert.IsTrue(simpleQueueBuffer.CanPull);
        }

        [Test]
        public async Task ShouldDelayPullWhenSlidingInEffect()
        {
            var simpleQueueBuffer = new SimpleQueueBuffer(3, 2, 1);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsFalse(simpleQueueBuffer.CanPull);

            simpleQueueBuffer.Push(new TestEventBusMessage(EventA.New()));
            simpleQueueBuffer.Push(new TestEventBusMessage(EventA.New()));
            simpleQueueBuffer.Push(new TestEventBusMessage(EventA.New()));

            await Task.Delay(1000);

            var messages = simpleQueueBuffer.Pull();
            Assert.IsNotEmpty(messages);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsFalse(simpleQueueBuffer.CanPull);

            simpleQueueBuffer.Push(new TestEventBusMessage(EventA.New()));
     
            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsFalse(simpleQueueBuffer.CanPull);

            //buffer expire
            await Task.Delay(2000);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsTrue(simpleQueueBuffer.CanPull);

            simpleQueueBuffer.Push(new TestEventBusMessage(EventA.New()));

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsFalse(simpleQueueBuffer.CanPull);

            await Task.Delay(1000);

            Assert.IsTrue(simpleQueueBuffer.CanPush);
            Assert.IsTrue(simpleQueueBuffer.CanPull);

        }

    }
}
