using Anabasis.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Demo.AspNet
{
    public class MarketDataBus : IBus
    {
        public string BusId => $"{nameof(MarketDataBus)}{Guid.NewGuid()}";

        public bool IsConnected => true;

        public bool IsInitialized => true;

        public void Dispose()
        {
        }

        public Task<HealthCheckResult> GetHealthCheck(bool shouldThrowIfUnhealthy = false)
        {
            return Task.FromResult(HealthCheckResult.Healthy("healthcheck from MarketDataBus", new Dictionary<string, object>()
            {
                {"MarketDataBus", "ok"}
            }));
        }

        public Task Initialize()
        {
            return Task.CompletedTask;
        }
    }
}
