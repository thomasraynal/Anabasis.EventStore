using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Common.HealthChecks
{
    public class DynamicHealthCheckProvider : IDynamicHealthCheckProvider
    {

        private List<HealthCheckRegistration> _healthCheckRegistrations = new List<HealthCheckRegistration>();

        public void AddHealthCheck(HealthCheckRegistration healthCheckRegistration)
        {
            _healthCheckRegistrations.Add(healthCheckRegistration);
        }

        public Task<HealthReport> CheckHealth(CancellationToken cancellationToken = default)
        {
            var now = DateTime.UtcNow;

            throw new NotImplementedException();
        }
    }
}
