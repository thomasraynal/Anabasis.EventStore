using Anabasis.Common.Contracts;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Configuration
{
    public interface IWorkerConfigurationFactory
    {
        void AddConfiguration<TWorker>(IWorkerConfiguration workerConfiguration);
        IWorkerConfiguration GetConfiguration(Type type);
    }
}
