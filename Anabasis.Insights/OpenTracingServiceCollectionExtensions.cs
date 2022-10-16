using Honeycomb.OpenTelemetry;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;
using System;

namespace Anabasis.Insights
{
    public static class OpenTracingServiceCollectionExtensions
    {
        public static IServiceCollection AddOpenTracing(this IServiceCollection serviceCollection, HoneycombOptions honeycombOptions, Action<TracerProviderBuilder>? configureTracerProviderBuilder = null)
        {

            serviceCollection.AddOpenTelemetryTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder.AddHoneycomb(honeycombOptions)
                           .AddAspNetCoreInstrumentationWithBaggage();

                configureTracerProviderBuilder?.Invoke(tracerProviderBuilder);

            });

            serviceCollection.AddSingleton(TracerProvider.Default.GetTracer(honeycombOptions.ServiceName));
      
            return serviceCollection;
        }
    }
}
