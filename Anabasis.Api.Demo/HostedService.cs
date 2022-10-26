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

        private IDisposable _disposable;

        public Task StartAsync(CancellationToken cancellationToken)
        {

             var httpClient = new HttpClient();

            _disposable = Observable.Interval(TimeSpan.FromMilliseconds(2000)).Subscribe(async _ =>
            {
                await httpClient.PutAsync("http://localhost/event", null);
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
