using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public interface IBus : IDisposable
    {
        string BusId { get; }
        bool IsConnected { get; }
        bool IsInitialized { get; }
        Task<HealthCheckResult> GetHealthCheck(bool shouldThrowIfUnhealthy = false);
        Task Initialize();
    }
}
