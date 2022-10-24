using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.Common.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Api.Demo
{
    public class WorkerOne : BaseWorker
    {
        public WorkerOne(IWorkerConfigurationFactory workerConfigurationFactory, IWorkerMessageDispatcherStrategy workerMessageDispatcherStrategy = null, ILoggerFactory loggerFactory = null) : base(workerConfigurationFactory, workerMessageDispatcherStrategy, loggerFactory)
        {
        }

        public WorkerOne(IWorkerConfiguration workerConfiguration, IWorkerMessageDispatcherStrategy workerMessageDispatcherStrategy = null, ILoggerFactory loggerFactory = null) : base(workerConfiguration, workerMessageDispatcherStrategy, loggerFactory)
        {
        }

        public override Task Handle(IEvent[] messages)
        {

            foreach(var message in messages)
            {
                Logger.LogInformation($"Handling {message.EventId}");
            }

            return Task.CompletedTask;
        }
    }
}
