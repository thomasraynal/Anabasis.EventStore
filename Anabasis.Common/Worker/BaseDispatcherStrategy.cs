using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common.Worker
{
    public abstract class BaseDispatcherStrategy : IWorkerMessageDispatcherStrategy
    {

        public IWorkerDispatchQueue[] WorkerDispatchQueues { get; private set; }

        protected BaseDispatcherStrategy()
        {
            WorkerDispatchQueues = Array.Empty<IWorkerDispatchQueue>();
        }

        public void Initialize(IWorkerDispatchQueue[] workerDispatchQueues)
        {
            WorkerDispatchQueues = workerDispatchQueues;

            OnInitialize();
        }

        protected abstract void OnInitialize();

        public abstract IWorkerDispatchQueue Next(int timeoutInSeconds = 30);


    }
}
