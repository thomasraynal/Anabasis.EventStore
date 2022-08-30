using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common.Worker
{
    public interface IWorkerMessageDispatcherStrategy
    {
        void Initialize(IWorkerDispatchQueue[] workerDispatchQueues);
        IWorkerDispatchQueue[] WorkerDispatchQueues { get; }
        Task<(bool isDispatchQueueAvailable, IWorkerDispatchQueue? workerDispatchQueue)> Next(double timeoutInSeconds = 30.0);
    }
}
