using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.Common.Worker;
using Anabasis.Insights;
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
        private readonly ITracer _tracer;

        public WorkerOne(ITracer tracer, IWorkerConfigurationFactory workerConfigurationFactory, IWorkerMessageDispatcherStrategy workerMessageDispatcherStrategy = null, ILoggerFactory loggerFactory = null) : base(workerConfigurationFactory, workerMessageDispatcherStrategy, loggerFactory)
        {
            _tracer = tracer;
        }

        public WorkerOne(ITracer tracer, IWorkerConfiguration workerConfiguration, IWorkerMessageDispatcherStrategy workerMessageDispatcherStrategy = null, ILoggerFactory loggerFactory = null) : base(workerConfiguration, workerMessageDispatcherStrategy, loggerFactory)
        {
            _tracer = tracer;
        }

        public override Task Handle(IEvent[] messages)
        {

            foreach (var message in messages.Cast<EventCreated>())
            {
                using var mainSpan = _tracer.StartActiveSpan("WorkerOne", traceId: message.TraceId.Value, startTime: DateTime.UtcNow);

                mainSpan.AddEvent("HandleEventStart");

                Logger.LogInformation($"Handling {message.EventId}");

                mainSpan.AddEvent("HandleEventEnd");
            }

            return Task.CompletedTask;
        }
    }
}
