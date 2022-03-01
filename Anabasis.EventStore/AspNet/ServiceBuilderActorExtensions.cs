using EventStore.ClientAPI;
using Microsoft.Extensions.DependencyInjection;
using System;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Actor;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using EventStore.Core;
using EventStore.ClientAPI.Embedded;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Anabasis.Common.HealthChecks;
using Anabasis.Common;
using Anabasis.EventStore.AspNet.Builders;

namespace Anabasis.EventStore
{

    public static class ServiceBuilderActorExtensions
    {

        public static IApplicationBuilder UseWorld(this IApplicationBuilder applicationBuilder)
        {
            var registerStreams = new Action<IConnectionStatusMonitor<IEventStoreConnection>, IEventStoreStatelessActorBuilder, Type>((connectionStatusMonitor, builder, actorType) =>
             {
                 var actor = (IEventStoreStatelessActor)applicationBuilder.ApplicationServices.GetService(actorType);

                 var loggerFactory = applicationBuilder.ApplicationServices.GetService<ILoggerFactory>();

                 foreach (var getStream in builder.GetStreamFactories())
                 {
                     var eventStoreStream = getStream(connectionStatusMonitor, loggerFactory);

                     actor.SubscribeToEventStream(eventStoreStream, closeSubscriptionOnDispose: true);
                 }

             });

            var registerHealthChecksAndBus = new Action<IStatelessActorBuilder, Type>((builder, actorType) =>
            {
                var actor = (IActor)applicationBuilder.ApplicationServices.GetService(actorType);

                var healthCheckService = applicationBuilder.ApplicationServices.GetService<IDynamicHealthCheckProvider>();
                healthCheckService.AddHealthCheck(new HealthCheckRegistration(actor.Id, actor, HealthStatus.Unhealthy, null));

                foreach (var busConfiguration in builder.GetBusFactories())
                {
                    busConfiguration.factory(applicationBuilder.ApplicationServices, actor);
                }

            });

            var initializeActor = new Action<IStatelessActorBuilder, Type>((builder, actorType) =>
            {
                var actor = (IActor)applicationBuilder.ApplicationServices.GetService(actorType);

                actor.OnInitialized().Wait();

            });

            var connectionStatusMonitor = applicationBuilder.ApplicationServices.GetService<IConnectionStatusMonitor<IEventStoreConnection>>();

            var world = applicationBuilder.ApplicationServices.GetService<World>();

            foreach (var (actorType, builder) in world.EventStoreStatelessActorBuilders)
            {
                registerHealthChecksAndBus(builder, actorType);
                registerStreams(connectionStatusMonitor, builder, actorType);
                initializeActor(builder, actorType);
            }

            foreach (var (actorType, builder) in world.EventStoreStatefulActorBuilders)
            {
                registerHealthChecksAndBus(builder, actorType);
                registerStreams(connectionStatusMonitor, builder, actorType);
                initializeActor(builder, actorType);
            }

            foreach (var (actorType, builder) in world.StatelessActorBuilders)
            {
                registerHealthChecksAndBus(builder, actorType);
                initializeActor(builder, actorType);
            }

            return applicationBuilder;
        }

        public static World AddWorld(this IServiceCollection services)
        {
            var world = new World(services, false);

            services.AddSingleton(world);

            return world;

        }

        public static World AddWorld(this IServiceCollection services,
            string eventStoreConnectionString,
            ConnectionSettingsBuilder connectionSettingsBuilder,
            Action<IEventStoreRepositoryConfiguration> getEventStoreRepositoryConfiguration = null)
        {

            var eventStoreConnection = EventStoreConnection.Create(eventStoreConnectionString, connectionSettingsBuilder);

            var connectionSettings = connectionSettingsBuilder.Build();

            services.AddSingleton<IConnectionStatusMonitor<IEventStoreConnection>, EventStoreConnectionStatusMonitor>();
            services.AddSingleton(eventStoreConnection);
            services.AddSingleton(connectionSettings);

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();

            getEventStoreRepositoryConfiguration?.Invoke(eventStoreRepositoryConfiguration);

            services.AddSingleton<IEventStoreRepositoryConfiguration>(eventStoreRepositoryConfiguration);

            services.AddTransient<IEventStoreRepository, EventStoreRepository>();

            var world = new World(services, true);

            services.AddSingleton(world);

            return world;
        }

        public static World AddWorld(this IServiceCollection services,
            Uri eventStoreUrl,
            ConnectionSettings connectionSettings,
            Action<IEventStoreRepositoryConfiguration> getEventStoreRepositoryConfiguration = null)
        {

            var eventStoreConnection = EventStoreConnection.Create(connectionSettings, eventStoreUrl);

            services.AddSingleton<IConnectionStatusMonitor<IEventStoreConnection>, EventStoreConnectionStatusMonitor>();
            services.AddSingleton(eventStoreConnection);
            services.AddSingleton(connectionSettings);

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();

            getEventStoreRepositoryConfiguration?.Invoke(eventStoreRepositoryConfiguration);

            services.AddSingleton<IEventStoreRepositoryConfiguration>(eventStoreRepositoryConfiguration);

            services.AddTransient<IEventStoreRepository, EventStoreRepository>();

            var world = new World(services, true);

            services.AddSingleton(world);

            return world;

        }

        public static World AddWorld(this IServiceCollection services,
            ClusterVNode clusterVNode,
            ConnectionSettings connectionSettings,
            Action<IEventStoreRepositoryConfiguration> getEventStoreRepositoryConfiguration = null)
        {

            var eventStoreConnection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);

            services.AddSingleton<IConnectionStatusMonitor<IEventStoreConnection>, EventStoreConnectionStatusMonitor>();
            services.AddSingleton(eventStoreConnection);
            services.AddSingleton(connectionSettings);

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();

            getEventStoreRepositoryConfiguration?.Invoke(eventStoreRepositoryConfiguration);

            services.AddSingleton<IEventStoreRepositoryConfiguration>(eventStoreRepositoryConfiguration);

            services.AddTransient<IEventStoreRepository, EventStoreRepository>();

            var world = new World(services, true);

            services.AddSingleton(world);

            return world;

        }

    }
}
