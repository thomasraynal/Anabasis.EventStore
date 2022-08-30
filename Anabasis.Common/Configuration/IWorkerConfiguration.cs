using Anabasis.Common.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Configuration
{
    public interface IWorkerConfiguration
    {
        int DispatcherCount { get; set; }
        int MessageBufferMaxSize { get; set; }
        double MessageBufferAbsoluteTimeoutInSecond { get; set; }
        double MessageBufferSlidingTimeoutInSecond { get; set; }
        bool SwallowUnknownEvent { get; set; }
        bool CrashAppOnFailure { get; set; }
        IDispacherStrategy DispacherStrategy { get; set; }
    }
}
