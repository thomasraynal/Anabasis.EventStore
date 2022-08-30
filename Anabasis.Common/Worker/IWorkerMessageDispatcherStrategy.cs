using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Worker
{
    public interface IWorkerMessageDispatcherStrategy
    {
        void Initialize(IWorkerDispatchQueue[] workerDispatchQueues);
        IWorkerDispatchQueue[] WorkerDispatchQueues { get; }
        IWorkerDispatchQueue Next(int timeoutInSeconds = 30);
    }
}
