using Anabasis.Common.Shared;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    //Initialize method
    public interface IBus : IDisposable
    {
        string BusId { get; }
        bool IsConnected { get; }
        Task<HealthCheckResult> GetHealthCheck(bool shouldThrowIfUnhealthy = false);
    }
}
