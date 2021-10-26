using Anabasis.EventStore.Demo.AspNet;
using Anabasis.EventStore.EventProvider;
using Carter;
using EventStore.ClientAPI;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Demo
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCarter();
            services.AddControllers();
            services.AddLogging();

            var connectionSettings = ConnectionSettings
                    .Create()
                    .DisableTls()
                    .DisableServerCertificateValidation()
                    .EnableVerboseLogging()
                    .UseDebugLogger()
                    .SetDefaultUserCredentials(StaticData.UserCredentials)
                    .Build();

            var tradeDataEventProvider = new DefaultEventTypeProvider<long, Trade>(() => new[] { typeof(TradeCreated), typeof(TradeStatusChanged) });
            var marketDataEventProvider = new DefaultEventTypeProvider<string, MarketData>(() => new[] { typeof(MarketDataChanged) });

            services.AddWorld(StaticData.ClusterVNode, connectionSettings)

                    .AddStatelessActor<MarketDataService>()
                    .CreateActor()

                    .AddStatelessActor<TradeService>()
                    .WithSubscribeFromEndToAllQueue()
                    .CreateActor()

                    .AddStatefulActor<TradePriceUpdateService, long, Trade>()
                    .WithReadAllFromStartCache(eventTypeProvider: tradeDataEventProvider)
                    .WithSubscribeFromEndToAllQueue()
                    .CreateActor()

                    .AddStatefulActor<TradeSink, long, Trade>()
                    .WithReadAllFromStartCache(eventTypeProvider: tradeDataEventProvider)
                    .CreateActor()

                    .AddStatefulActor<MarketDataSink, string, MarketData>()
                    .WithReadAllFromStartCache(eventTypeProvider: marketDataEventProvider)
                    .CreateActor();


            services.AddHostedService<HostedService>();

        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseWorld();
            
            app.UseRouting();
            app.UseEndpoints(builder =>
            {
                builder.MapDefaultControllerRoute();
                builder.MapCarter();
            });
        }
    }
}
