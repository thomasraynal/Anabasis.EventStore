using System;
using System.Threading.Tasks;
using BeezUP2.Framework.Application;
using BeezUP2.Framework.Handlers;
using BeezUP2.Framework.Insights.HealthCheck;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using RetryPolicy = Microsoft.Azure.EventHubs.RetryPolicy;

namespace BeezUP2.Framework.Configuration
{
    public static class EventHubProcessorHostParametersExtensions
    {
        public static EventHubProcessorHostParameters WithHealthCheck(this EventHubProcessorHostParameters settings, BeezUPApp app)
        {
            var hcName = $"EventHubs processor host {settings.Connection.Namespace}.{settings.Connection.HubName} connection";

            if (!app.AddDoubleKeyIfNotExisting(hcName))
                return settings;

            settings.CheckAsync().Wait();

            app.HealthChecksBuilder
                .AddAsyncCheck(hcName, async () =>
                {
                    try
                    {
                        await settings.CheckAsync();
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

        public static async Task CheckAsync(this EventHubProcessorHostParameters settings)
        {
            var connectionString = settings.Connection.GetConnectionString();
            var conBuilder = new ServiceBusConnectionStringBuilder(connectionString) { TransportType = Microsoft.Azure.ServiceBus.TransportType.Amqp, EntityPath = settings.Connection.HubName };
            var client = EventHubClient.CreateFromConnectionString(conBuilder.GetEntityConnectionString());
            client.RetryPolicy = RetryPolicy.Default;

            await client.GetRuntimeInformationAsync();
            await settings.EventHubConsumerSettings.CheckAsync();
            // TODO not able to check consumer group /!\
        }
    }
}
