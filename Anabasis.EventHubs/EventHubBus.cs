using Anabasis.Common;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventHubs
{
    public class EventHubBus : IEventHubBus
    {
        private readonly EventHubConnectionOptions _eventHubConnectionOptions;

        public EventHubBus(EventHubConnectionOptions eventHubConnectionOptions)
        {
            _eventHubConnectionOptions = eventHubConnectionOptions;
        }

        public string BusId => throw new NotImplementedException();

        public IConnectionStatusMonitor ConnectionStatusMonitor => throw new NotImplementedException();

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = GetEventHubClient();
                await CheckAsync(client);

                return HealthCheckResult.Healthy($"EventHub bus {_eventHubConnectionOptions.Namespace}.{_eventHubConnectionOptions.HubName} is healthy");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"EventHub bus {_eventHubConnectionOptions.Namespace}.{_eventHubConnectionOptions.HubName} is unhealthy - {ex.Message}", ex);
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

        private EventHubClient GetEventHubClient()
        {
            var connectionString = _eventHubConnectionOptions.GetConnectionString();

            var conBuilder = new ServiceBusConnectionStringBuilder(connectionString)
            {
                TransportType = Microsoft.Azure.ServiceBus.TransportType.Amqp,
                EntityPath = _eventHubConnectionOptions.HubName,
                OperationTimeout = TimeSpan.FromSeconds(30)
            };

            var client = EventHubClient.CreateFromConnectionString(conBuilder.GetEntityConnectionString());
            client.RetryPolicy = Microsoft.Azure.EventHubs.RetryPolicy.Default;
            return client;
        }
    }
}
