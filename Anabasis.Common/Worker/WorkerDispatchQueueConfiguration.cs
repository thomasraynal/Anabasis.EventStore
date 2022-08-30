using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common.Worker
{
    public class WorkerDispatchQueueConfiguration
    {
        public WorkerDispatchQueueConfiguration(Func<IEvent[], Task> onEventsReceived, bool crashAppOnError)
        {
            OnEventsReceived = onEventsReceived;
            CrashAppOnError = crashAppOnError;
        }

        public int MessageBufferMaxSize { get; set; } = 1;
        public double MessageBufferAbsoluteTimeoutInSecond { get; set; } = 0;
        public double MessageBufferSlidingTimeoutInSecond { get; set; } = 0;

        public Func<IEvent[], Task> OnEventsReceived { get; }
        public bool CrashAppOnError { get; }
    }
}
