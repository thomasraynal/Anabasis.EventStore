using Anabasis.Common.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Configuration
{

    public class WorkerConfiguration : IWorkerConfiguration
    {
        public int DispatcherCount { get; set; } = 1;
        public bool SwallowUnknownEvent { get; set; } = true;
        public bool CrashAppOnFailure { get; set; } = true;
        public int MessageBufferMaxSize { get; set; } = 1;
        public double MessageBufferAbsoluteTimeoutInSecond { get; set; } = 0;
        public double MessageBufferSlidingTimeoutInSecond { get; set; } = 0;
    }

}
