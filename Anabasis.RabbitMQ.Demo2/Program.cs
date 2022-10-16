using Anabasis.Api;
using Anabasis.Common;
using Anabasis.EventStore;
using Anabasis.EventStore.AspNet;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.RabbitMQ.Demo;
using EventStore.ClientAPI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Anabasis.RabbitMQ.Demo2
{


    public class Program
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

                            var marketDataEventHandler = new DefaultEventTypeProvider<Product>(() => new[] { typeof(ProductChanged) });

                            serviceCollection.WithConfiguration<RabbitMqConnectionOptions>(configurationRoot);
                            serviceCollection.WithConfiguration<EventStoreConnectionOptions>(configurationRoot);

                            serviceCollection.AddSingleton<IRabbitMqBus, RabbitMqBus>();
                            serviceCollection.AddSingleton<IEventStoreBus, EventStoreBus>();

                            serviceCollection.AddWorld("ConnectTo=tcp://admin:changeit@localhost:1113; HeartBeatTimeout=1500; VerboseLogging=false; OperationTimeout=60000; UseSslConnection=false;", connectionSettingsBuilder)

                                             .AddEventStoreStatefulActor<ProductInventoryActor, Product, AllStreamsCatchupCacheConfiguration>(
                                                eventTypeProvider: marketDataEventHandler,
                                                getAggregateCacheConfiguration: (conf) => conf.Checkpoint = Position.Start)
                                             .WithBus<IEventStoreBus>()
                                             .WithBus<IRabbitMqBus>((actor, bus) =>
                                             {
                                                 actor.SubscribeToExchange<ProductInventoryChanged>(StaticData.ProducyInventoryExchange,
                                                     queueName : "ProductInventoryActor",
                                                     isQueueDurable: true,
                                                     isQueueAutoDelete: false,
                                                     isQueueExclusive: false);
                                             })
                                             .CreateActor();

                            serviceCollection.AddHostedService<ChangeProductHostedService>();

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
