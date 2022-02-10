using Anabasis.Common.HealthChecks;
using Anabasis.Common.Utilities;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Api
{
    public class HealthCheckHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<HealthCheckHostedService> _logger;
        private IDisposable _doHealthCheck;
        private HealthStatus? _previousStatus = null;

        public HealthCheckHostedService(IServiceProvider serviceProvider, ILogger<HealthCheckHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {

            await DoHealthCheck(cancellationToken);

            _doHealthCheck = TaskPoolScheduler.Default
                .ScheduleRecurringAction(TimeSpan.FromSeconds(10), async () => await DoHealthCheck(cancellationToken));
        }

        private async Task DoHealthCheck(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            var healthCheckService = _serviceProvider.GetRequiredService<HealthCheckService>();

            var healthCheckServiceHealthReport = await healthCheckService.CheckHealthAsync(cancellationToken);

            var dynamicHealthCheckProvider = _serviceProvider.GetRequiredService<IDynamicHealthCheckProvider>();

            if (null != dynamicHealthCheckProvider)
            {
                var dynamicHealthCheckProviderHealthReport = await dynamicHealthCheckProvider.CheckHealth(cancellationToken);

                healthCheckServiceHealthReport = healthCheckServiceHealthReport.Combine(dynamicHealthCheckProviderHealthReport);

            }

            var currentStatus = healthCheckServiceHealthReport.Status;
            var statusChanged = _previousStatus != currentStatus;
            var notHealthy = currentStatus != HealthStatus.Healthy;

            var show = notHealthy || statusChanged;

            if (show)
            {

                if (statusChanged)
                {
                    _logger.LogInformation($"================> HEALTH STATUS CHANGED from {_previousStatus?.ToString() ?? "<unknown>"} to {currentStatus}");
                }

                _previousStatus = currentStatus;

                if (notHealthy)
                {
                    _logger.LogInformation($"================> HEALTH STATUS : { currentStatus.ToString().ToUpper() } =================");

                    foreach (var entry in healthCheckServiceHealthReport.Entries)
                    {
                        var key = entry.Key;
                        var value = entry.Value;

                        _logger.LogInformation($"{key}: [{value.Status}] {value.Description}");
                    }
                }

            }
        }
           
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _doHealthCheck.Dispose();

            return Task.CompletedTask;
        }
    }
}
