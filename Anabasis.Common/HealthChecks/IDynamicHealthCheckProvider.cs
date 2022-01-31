using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Common.HealthChecks
{
    public interface IDynamicHealthCheckProvider
    {
        void AddHealthCheck(HealthCheckRegistration healthCheckRegistration);
        Task<HealthReport> CheckHealth(CancellationToken cancellationToken = default);
    }

}
