﻿using Microsoft.AspNetCore.Hosting;
using Anabasis.Api;
using EventStore.ClientAPI;
using Microsoft.Extensions.DependencyInjection;
using Anabasis.Common;
using Anabasis.EventStore.Demo.AspNet;
using Microsoft.AspNetCore.Builder;
using Anabasis.EventStore.Demo.Bus;
using Anabasis.EventStore.AspNet;
using Anabasis.EventStore.Cache;

namespace Anabasis.EventStore.Demo
{
    class Program
    {
        static void Main(string[] _)
        {
            {
                WebAppBuilder.Create<Program>(
                        configureServiceCollection: (anabasisContext, serviceCollection, configurationRoot) =>
                        {

                            var connectionSettings = ConnectionSettings
                                    .Create()
                                    .DisableTls()
                                    .DisableServerCertificateValidation()
                                    .EnableVerboseLogging()
                                    .UseDebugLogger()
                                    .KeepReconnecting()
                                    .KeepRetrying()
                                    .SetDefaultUserCredentials(StaticData.UserCredentials);

                            var tradeDataEventProvider = new DefaultEventTypeProvider<Trade>(() => new[] { typeof(TradeCreated), typeof(TradeStatusChanged) });
                            var marketDataEventProvider = new DefaultEventTypeProvider<MarketData>(() => new[] { typeof(MarketDataChanged) });
                            
                            var allStreamsCatchupCacheConfiguration = new AllStreamsCatchupCacheConfiguration();

                            serviceCollection.AddSingleton<IMarketDataBus, MarketDataBus>();
                            serviceCollection.AddSingleton<IEventStoreBus, EventStoreBus>();

                            serviceCollection.AddWorld("ConnectTo=tcp://admin:changeit@localhost:1113; HeartBeatTimeout=1500; VerboseLogging=false; OperationTimeout=60000; UseSslConnection=false;", connectionSettings)

                                    .AddStatelessActor<TradeService>(ActorConfiguration.Default)
                                        .WithBus<IEventStoreBus>((actor, bus) =>
                                        {
                                            actor.SubscribeToAllStreams(Position.Start);
                                        })
                                        .CreateActor()

                                    .AddEventStoreStatefulActor<TradePriceUpdateService, Trade, AllStreamsCatchupCacheConfiguration>(tradeDataEventProvider)
                                        .WithBus<IEventStoreBus>((actor, bus) =>
                                        {
                                            actor.SubscribeToAllStreams(Position.Start);
                                        })
                                        .WithBus<IMarketDataBus>((actor, bus) =>
                                        {
                                            actor.SubscribeMarketDataBus();
                                        })
                                        .CreateActor()

                                    .AddEventStoreStatefulActor<TradeSink, Trade, AllStreamsCatchupCacheConfiguration>(tradeDataEventProvider)
                                        .CreateActor()

                                    .AddStatelessActor<MarketDataSink>(ActorConfiguration.Default)
                                        .WithBus<IMarketDataBus>((actor, bus) =>
                                        {
                                            actor.SubscribeMarketDataBus();
                                        })
                                        .CreateActor();


                            serviceCollection.AddHostedService<HostedService>();

                        },
                        configureApplicationBuilder: (anabasisContext, app) =>
                        {
                            app.UseWorld();
                        })
                        .Build()
                        .Run();
            }
        }
    }
}
