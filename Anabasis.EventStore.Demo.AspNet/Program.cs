using Microsoft.AspNetCore.Hosting;
using Anabasis.Api;
using Anabasis.EventStore.EventProvider;
using EventStore.ClientAPI;
using Microsoft.Extensions.DependencyInjection;
using Anabasis.Common;
using Anabasis.EventStore.Demo.AspNet;
using Microsoft.AspNetCore.Builder;
using Anabasis.EventStore.Demo.Bus;

namespace Anabasis.EventStore.Demo
{
    class Program
    {
        static void Main(string[] _)
        {
            {
                WebAppBuilder.Create<Program>(
                        configureServiceCollection: (serviceCollection, configurationRoot) =>
                        {
          
                            var connectionSettings = ConnectionSettings
                                    .Create()
                                    .DisableTls()
                                    .DisableServerCertificateValidation()
                                    .EnableVerboseLogging()
                                    .UseDebugLogger()
                                    .SetDefaultUserCredentials(StaticData.UserCredentials)
                                    .Build();

                            var tradeDataEventProvider = new DefaultEventTypeProvider<Trade>(() => new[] { typeof(TradeCreated), typeof(TradeStatusChanged) });
                            var marketDataEventProvider = new DefaultEventTypeProvider<MarketData>(() => new[] { typeof(MarketDataChanged) });

                            serviceCollection.AddSingleton<IMarketDataBus, MarketDataBus>();

                            serviceCollection.AddWorld(StaticData.ClusterVNode, connectionSettings)

               
                                    .AddEventStoreStatelessActor<TradeService>(ActorConfiguration.Default)
                                        .WithSubscribeFromEndToAllStreams()
                                        .CreateActor()

                                    .AddEventStoreStatefulActor<TradePriceUpdateService, Trade>(ActorConfiguration.Default)
                                        .WithReadAllFromStartCache(eventTypeProvider: tradeDataEventProvider)
                                        .WithSubscribeFromEndToAllStreams()
                                        .WithBus<IMarketDataBus>((actor, bus) =>
                                        {
                                            actor.SubscribeMarketDataBus();
                                        })
                                        .CreateActor()

                                    .AddEventStoreStatefulActor<TradeSink, Trade>(ActorConfiguration.Default)
                                        .WithReadAllFromStartCache(eventTypeProvider: tradeDataEventProvider)
                                        .CreateActor()

                                    .AddStatelessActor<MarketDataSink>(ActorConfiguration.Default)
                                        .WithBus<IMarketDataBus>((actor, bus) =>
                                        {
                                            actor.SubscribeMarketDataBus();
                                        })
                                        .CreateActor();


                            serviceCollection.AddHostedService<HostedService>();

                        },
                        configureApplicationBuilder: (app) =>
                        {
                            app.UseWorld();
                        })
                        .Build()
                        .Run();
            }
        }
    }
}
