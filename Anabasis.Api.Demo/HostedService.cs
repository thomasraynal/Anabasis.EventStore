﻿using Anabasis.Insights;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Api.Demo
{
    public class HostedService : IHostedService
    {
        private IDisposable _disposable;
        private readonly ITracer _tracer;

        public HostedService(ITracer tracer)
        {
               _tracer = tracer;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {

            var traceId = Guid.NewGuid();

            _disposable = Observable.Interval(TimeSpan.FromMilliseconds(2000)).Subscribe(async _ =>
            {


                using (var mainSpan = _tracer.StartActiveSpan("process", traceId,  startTime: DateTime.UtcNow))
                {
                    mainSpan.SetAttribute("delay_ms", 100);
                    await Task.Delay(500);

                    mainSpan.AddEvent("one");

                    using (var childSpan = _tracer.StartSpan("childProcess", traceId, startTime: DateTime.UtcNow))
                    {
                        await Task.Delay(1000);

                        mainSpan.AddEvent("tow");
                    }
                }
            });

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _disposable.Dispose();

            return Task.CompletedTask;
        }
    }
}
