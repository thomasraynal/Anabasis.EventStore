using Anabasis.Common.Configuration;
using Anabasis.Insights;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Api.Demo
{
    public class HostedService : IHostedService
    {
        private readonly WorkerOne _workerOne;
        private readonly IBusOne _busOne;
        private IDisposable _disposable;

        public HostedService(IBusOne busOne, ITracer tracer, ILoggerFactory loggerFactory)
        {

            var workerConfiguration = new WorkerConfiguration()
            {
                DispatcherCount = 2,
            };

            _workerOne = new WorkerOne(tracer, workerConfiguration,loggerFactory: loggerFactory);

            _busOne = busOne;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {

            await _workerOne.ConnectTo(_busOne);

            await _workerOne.WaitUntilConnected();

            _busOne.Subscribe((@event) =>
            {
                _workerOne.Handle(new[] { @event });
            });

             var httpClient = new HttpClient();

            _disposable = Observable.Interval(TimeSpan.FromMilliseconds(2000)).Subscribe(async _ =>
            {
                await httpClient.PutAsync("http://localhost/event", null);
            });

        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _disposable.Dispose();

            return Task.CompletedTask;
        }
    }
}
