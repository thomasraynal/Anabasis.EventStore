using System;
using System.Threading.Tasks;
using BeezUP2.Framework.Application;
using BeezUP2.Framework.Handlers;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RetryPolicy = Microsoft.Azure.EventHubs.RetryPolicy;

namespace BeezUP2.Framework.Configuration
{
    public static class EventHubConnectionSettingsExtensions
    {
        public static EventHubConnectionSettings WithHealthCheck(this EventHubConnectionSettings settings, BeezUPApp app)
        {
            var hcName = $"EventHubs {settings.Namespace}.{settings.HubName} connection";

            if (!app.AddDoubleKeyIfNotExisting(hcName))
                return settings;

            var client = GetClient(settings);
            client.CheckAsync().Wait();

            app.HealthChecksBuilder
                .AddAsyncCheck(hcName, async () =>
                {
                    try
                    {
                        await client.CheckAsync().CAF();
                        return HealthCheckResult.Healthy();
                    }
                    catch (Exception ex)
                    {
                        return HealthCheckResult.Unhealthy(ex.Message, ex);
                    }
                },
                StartablePriority.Infrastructure
                );

            return settings;
        }

        public static async Task CheckAsync(this EventHubConnectionSettings settings)
        {
            var client = GetClient(settings);
            await CheckAsync(client);
        }

        private static async Task CheckAsync(this EventHubClient client)
        {
            await client.GetRuntimeInformationAsync().CAF();
        }

        private static EventHubClient GetClient(EventHubConnectionSettings settings)
        {
            var connectionString = settings.GetConnectionString();
            var conBuilder = new ServiceBusConnectionStringBuilder(connectionString) { TransportType = Microsoft.Azure.ServiceBus.TransportType.Amqp, EntityPath = settings.HubName, OperationTimeout = TimeSpan.FromSeconds(30) };
            var client = EventHubClient.CreateFromConnectionString(conBuilder.GetEntityConnectionString());
            client.RetryPolicy = RetryPolicy.Default;
            return client;
        }
    }
}
