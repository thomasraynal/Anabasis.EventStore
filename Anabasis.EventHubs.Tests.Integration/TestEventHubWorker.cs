using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.Common.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventHubs.Tests.Integration
{
    public class TestEventHubWorker : BaseWorker
    {
        public ConcurrentBag<IEvent> Events { get; private set; }

        public TestEventHubWorker(IWorkerConfigurationFactory workerConfigurationFactory, ILoggerFactory loggerFactory = null) : base(workerConfigurationFactory, loggerFactory)
        {
            Events = new ConcurrentBag<IEvent>();
        }

        public TestEventHubWorker(IWorkerConfiguration workerConfiguration, IWorkerMessageDispatcherStrategy workerMessageDispatcherStrategy = null, ILoggerFactory loggerFactory = null) : base(workerConfiguration, workerMessageDispatcherStrategy, loggerFactory)
        {
            Events = new ConcurrentBag<IEvent>();
        }

        public override Task Handle(IEvent[] messages)
        {
            foreach(var message in messages)
            {
                Events.Add(message);
            }

            return Task.CompletedTask;
        }
    }
}
