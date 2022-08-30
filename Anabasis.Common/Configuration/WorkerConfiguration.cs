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

        public IDispacherStrategy DispacherStrategy { get; set; } = new SingleDispatcherStrategy();
        public int MessageBufferMaxSize { get; set; }
        public double MessageBufferAbsoluteTimeoutInSecond { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public double MessageBufferSlidingTimeoutInSecond { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
