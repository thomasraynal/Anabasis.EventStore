using Anabasis.Common.Worker;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Configuration
{
    public class WorkerConfigurationFactory : IWorkerConfigurationFactory
    {
        private readonly Dictionary<Type, (IWorkerConfiguration workerConfiguration, IWorkerMessageDispatcherStrategy workerMessageDispatcherStrategy)> _workerConfigurations;

        public WorkerConfigurationFactory()
        {
            _workerConfigurations = new Dictionary<Type, (IWorkerConfiguration, IWorkerMessageDispatcherStrategy)>();
        }

        public void AddConfiguration<TWorker>(IWorkerConfiguration workerConfiguration, IWorkerMessageDispatcherStrategy workerMessageDispatcherStrategy)
        {
            _workerConfigurations.Add(typeof(TWorker), (workerConfiguration, workerMessageDispatcherStrategy));
        }

        public (IWorkerConfiguration workerConfiguration, IWorkerMessageDispatcherStrategy workerMessageDispatcherStrategy) GetConfiguration(Type type)
        {
            if (!_workerConfigurations.ContainsKey(type))
                throw new InvalidOperationException($"Unable to find a configuration {typeof(IWorkerConfiguration)} for worker {type}");

            return _workerConfigurations[type];
        }
    }
}
