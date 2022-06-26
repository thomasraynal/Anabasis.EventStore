using System;
using System.Threading.Tasks;
using Anabasis.EventHubs;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.ServiceBus;
using RetryPolicy = Microsoft.Azure.EventHubs.RetryPolicy;

namespace BeezUP2.Framework.Configuration
{
    public static class EventHubConnectionSettingsExtensions
    {
        public static EventHubConnectionOptions WithHealthCheck(this EventHubConnectionOptions settings)
        {
            var hcName = $"EventHubs {settings.Namespace}.{settings.HubName} connection";

            var client = GetClient(settings);

            client.CheckAsync().Wait();

            app.HealthChecksBuilder
                .AddAsyncCheck(hcName, async () =>
                {
                    try
                    {
                        await client.CheckAsync();
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


    }
}
