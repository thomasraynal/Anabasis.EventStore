using Anabasis.Common;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventHubs
{
    public class EventHubBus : IEventHubBus
    {
        public string BusId => throw new NotImplementedException();

        public IConnectionStatusMonitor ConnectionStatusMonitor => throw new NotImplementedException();

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = GetClient(settings);
                await CheckAsync(client);

                return HealthCheckResult.Healthy($"EventHub bus {settings.Namespace}.{settings.HubName} is healthy");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"EventHub bus {settings.Namespace}.{settings.HubName} is unhealthy", ex.Message, ex);
            }

        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public Task WaitUntilConnected(TimeSpan? timeout = null)
        {
            throw new NotImplementedException();
        }

        private static async Task CheckAsync(EventHubClient EventHubCliene)
        {
            await EventHubCliene.GetRuntimeInformationAsync();
        }

        private static EventHubClient GetClient(EventHubConnectionOptions settings)
        {
            var connectionString = settings.GetConnectionString();
            var conBuilder = new ServiceBusConnectionStringBuilder(connectionString) { TransportType = Microsoft.Azure.ServiceBus.TransportType.Amqp, EntityPath = settings.HubName, OperationTimeout = TimeSpan.FromSeconds(30) };
            var client = EventHubClient.CreateFromConnectionString(conBuilder.GetEntityConnectionString());
            client.RetryPolicy = Microsoft.Azure.EventHubs.RetryPolicy.Default;
            return client;
        }
    }
}
