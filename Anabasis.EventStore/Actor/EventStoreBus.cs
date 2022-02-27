using Anabasis.Common;
using EventStore.ClientAPI;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Actor
{
    public class EventStoreBus : IBus
    {
        public EventStoreBus(IConnectionStatusMonitor<IEventStoreConnection> connectionStatusMonitor)
        {
            BusId = $"{nameof(EventStoreBus)}_{Guid.NewGuid()}";
            ConnectionStatusMonitor = connectionStatusMonitor;
        }

        public string BusId { get; }

        public bool IsInitialized => true;

        public IConnectionStatusMonitor ConnectionStatusMonitor { get; }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var isConnected = ConnectionStatusMonitor.IsConnected;

            HealthCheckResult healthCheckResult = HealthCheckResult.Healthy();

            if (!isConnected) healthCheckResult = HealthCheckResult.Unhealthy("EventStore connection is down");

            return Task.FromResult(healthCheckResult);
        }

        public void Dispose()
        {
        }

        public Task Initialize()
        {
            return Task.CompletedTask;
        }
    }
}
