﻿using System;
using System.Threading.Tasks;

namespace Anabasis.Common.Worker
{
    public interface IWorkerDispatchQueueConfiguration
    {
        bool CrashAppOnError { get; }
        double MessageBufferAbsoluteTimeoutInSecond { get; set; }
        int MessageBufferMaxSize { get; set; }
        double MessageBufferSlidingTimeoutInSecond { get; set; }
        Func<IEvent[], Task> OnEventsReceived { get; }
    }
}