using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Common.HealthChecks
{
    public class DynamicHealthCheckProvider : IDynamicHealthCheckProvider
    {

        private readonly List<HealthCheckRegistration> _healthCheckRegistrations = new();
        private readonly IServiceProvider _serviceProvider;

        public DynamicHealthCheckProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void AddHealthCheck(HealthCheckRegistration healthCheckRegistration)
        {
            _healthCheckRegistrations.Add(healthCheckRegistration);
        }

        public async Task<HealthReport> CheckHealth(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            (HealthCheckRegistration healthCheckRegistration, IHealthCheck healthCheck)[] healthChecks =
                _healthCheckRegistrations.Select(healthCheckRegistration => (healthCheckRegistration, healthCheckRegistration.Factory(_serviceProvider))).ToArray();

            var healthReportEntries = new Dictionary<string, HealthReportEntry>();

            foreach (var (healthCheckRegistration, healthCheck) in healthChecks)
            {
                var healthCheckResult = await healthCheck.CheckHealthAsync(null, cancellationToken);

                var healthReportEntry = new HealthReportEntry(healthCheckResult.Status,
                    healthCheckResult.Description,
                    DateTime.UtcNow - now,
                    healthCheckResult.Exception,
                    healthCheckResult.Data);

                healthReportEntries.Add(healthCheckRegistration.Name, healthReportEntry);

            }

            var totalDuration = healthReportEntries.Select(healthReportEntry => healthReportEntry.Value.Duration)
                                                   .Aggregate((healthReportEntry1, healthReportEntry2) => healthReportEntry1 + healthReportEntry2);

            var healthStatus = healthReportEntries.Min(healthReportEntry => healthReportEntry.Value.Status);

            var healthReport = new HealthReport(healthReportEntries, healthStatus, totalDuration);

            return healthReport;

        }
    }
}
