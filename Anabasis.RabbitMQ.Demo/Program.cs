using Anabasis.Api;
using Anabasis.Common;
using Anabasis.EventStore;
using Anabasis.EventStore.AspNet;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using EventStore.ClientAPI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Anabasis.RabbitMQ.Demo
{
    class Program
    {
        static void Main(string[] _)
        {
            {
                WebAppBuilder.Create<Program>(
                        configureServiceCollection: (anabasisContext, serviceCollection, configurationRoot) =>
                        {
                            var connectionSettingsBuilder = ConnectionSettings
                                .Create()
                                .DisableTls()
                                .DisableServerCertificateValidation()
                                .EnableVerboseLogging()
                                .UseDebugLogger()
                                .KeepReconnecting()
                                .KeepRetrying()
                                .SetDefaultUserCredentials(StaticData.UserCredentials);

                            var connectionSettings = connectionSettingsBuilder.Build();

                            var marketDataEventHandler = new DefaultEventTypeProvider<MarketData>(() => new[] { typeof(MarketDataChanged) });

                            serviceCollection.WithConfiguration<RabbitMqConnectionOptions>(configurationRoot);
                            serviceCollection.WithConfiguration<EventStoreConnectionOptions>(configurationRoot);

                            serviceCollection.AddSingleton<IRabbitMqBus, RabbitMqBus>();
                            serviceCollection.AddSingleton<IEventStoreBus, EventStoreBus>();

                            serviceCollection.AddWorld("ConnectTo=tcp://admin:changeit@localhost:1113; HeartBeatTimeout=1500; VerboseLogging=false; OperationTimeout=60000; UseSslConnection=false;", connectionSettingsBuilder)

                                         .AddEventStoreStatefulActor<MarketDataActor, MarketData, AllStreamsCatchupCacheConfiguration>(
                                                eventTypeProvider: marketDataEventHandler,
                                                getAggregateCacheConfiguration: (conf) => conf.Checkpoint = Position.End)
                                         .WithBus<IEventStoreBus>()
                                         .WithBus<IRabbitMqBus>((actor, bus) =>
                                             {
                                                 actor.SubscribeToExchange<MarketDataQuoteChanged>(StaticData.MarketDataBusOne);
                                                 actor.SubscribeToExchange<MarketDataQuoteChanged>(StaticData.MarketDataBusTwo);
                                             })
                                         .CreateActor()

                                        .AddStatelessActor<MarketDataGenerator>(ActorConfiguration.Default)
                                         .WithBus<IEventStoreBus>()
                                         .WithBus<IRabbitMqBus>()
                                        .CreateActor();



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
