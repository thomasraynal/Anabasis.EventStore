using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common.Worker
{
    public class WorkerDispatchQueueConfiguration : IWorkerDispatchQueueConfiguration
    {
        public WorkerDispatchQueueConfiguration(Func<IEvent[], Task> onEventsReceived, 
            bool crashAppOnError = true,
            int messageBufferMaxSize = 1,
            int messageBufferAbsoluteTimeoutInSecond = 0,
            int messageBufferSlidingTimeoutInSecond = 0)
        {
            OnEventsReceived = onEventsReceived;
            CrashAppOnError = crashAppOnError;
            MessageBufferMaxSize = messageBufferMaxSize;
            MessageBufferAbsoluteTimeoutInSecond = messageBufferAbsoluteTimeoutInSecond;
            MessageBufferSlidingTimeoutInSecond = messageBufferSlidingTimeoutInSecond;
        }

        public int MessageBufferMaxSize { get; set; } = 1;
        public double MessageBufferAbsoluteTimeoutInSecond { get; set; } = 0;
        public double MessageBufferSlidingTimeoutInSecond { get; set; } = 0;
        public Func<IEvent[], Task> OnEventsReceived { get; }
        public bool CrashAppOnError { get; }
    }
}
