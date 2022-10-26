using Anabasis.Common.Contracts;
using Anabasis.Common.Worker;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Configuration
{
    public interface IWorkerConfigurationFactory
    {
        void AddConfiguration<TWorker>(IWorkerConfiguration workerConfiguration, IWorkerMessageDispatcherStrategy workerMessageDispatcherStrategy);
        (IWorkerConfiguration workerConfiguration, IWorkerMessageDispatcherStrategy workerMessageDispatcherStrategy) GetConfiguration(Type type);
    }
}
