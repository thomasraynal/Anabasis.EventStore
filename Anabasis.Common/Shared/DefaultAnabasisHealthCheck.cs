using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Shared
{
    public class DefaultAnabasisHealthCheck : IAnabasisHealthCheck
    {
        public DefaultAnabasisHealthCheck(HealthStatus healthStatus, string[] healthMessages = null)
        {
            HealthStatus = healthStatus;
            HealthMessages = healthMessages ?? new string[0];
        }

        public HealthStatus HealthStatus { get; }

        public string[] HealthMessages { get; }
    }
}
