//using NUnit.Framework;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using Anabasis.Common;

//namespace Anabasis.EventStore.Tests
//{
//    [TestFixture]
//    public class TestTaskExtensions
//    {
//        class TestSynchronizationContext : SynchronizationContext
//        {
//            public List<DateTime> ExecutionDates = new();

//            public override void Post(SendOrPostCallback d, object state)
//            {

//                ExecutionDates.Add(DateTime.UtcNow);
//                base.Post(d, state);

//            }
//        }

//        [Test]
//        public async Task ShouldRunTasksWithExecute()
//        {

//            var concurrent = 0;
//            var batchSize = 1;
//            var rand = new Random(1);

//            var tasks = Enumerable.Range(0, 10).Select(async(_) =>
//            {
//                try
//                {
//                    var currentRunningTaskCount = Interlocked.Increment(ref concurrent);

//                    Assert.True(currentRunningTaskCount < currentRunningTaskCount + 1);

//                    await Task.Delay(rand.Next(10));
//                }
//                finally
//                {
//                    Interlocked.Decrement(ref concurrent);
//                }

//            });

//            await tasks.Execute(batchSize);

//        }

//        [Test]
//        public async Task ShouldRunTasksWithExecuteAndNoTimeout()
//        {
//            var context = new TestSynchronizationContext();
//            SynchronizationContext.SetSynchronizationContext(context);


//            var task1 = new Task(() => Thread.Sleep(500));
//            var task2 = new Task(() => Thread.Sleep(500));

//            await new[] { task1, task2 }.Execute(1, TimeSpan.FromSeconds(5));

//            Assert.True(task1.Status == TaskStatus.RanToCompletion);
//            Assert.True(task2.Status == TaskStatus.RanToCompletion);

//        }

//        [Test]
//        public void ShouldRunTasksWithExecuteAndTimeout()
//        {
       
//            var task1 = new Task(() => Thread.Sleep(600));
//            var task2 = new Task(() => Thread.Sleep(2000));

//             Assert.ThrowsAsync<OperationCanceledException>(async () =>
//             {
//                 await new[] { task1, task2 }.Execute(1, TimeSpan.FromMilliseconds(500));
//             });

//            Assert.True(task1.Status == TaskStatus.RanToCompletion);
//            Assert.True(task2.Status == TaskStatus.Running);

//        }
//    }
//}
