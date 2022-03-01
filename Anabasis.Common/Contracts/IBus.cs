using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading.Tasks;

namespace Anabasis.Common
{
    public interface IBus : IHealthCheck, IDisposable
    {
        string BusId { get; }
        bool IsInitialized { get; }
        Task Initialize();
        IConnectionStatusMonitor ConnectionStatusMonitor { get; }
        Task WaitUntilConnected(TimeSpan? timeout = null);
    }
}
