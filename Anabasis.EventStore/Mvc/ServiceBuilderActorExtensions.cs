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

namespace Anabasis.EventStore
{

    public static class ServiceBuilderActorExtensions
    {

        private static World _world;

        public static IApplicationBuilder UseWorld(this IApplicationBuilder applicationBuilder)
        {
            var registerStreamsAndBus = new Action<IConnectionStatusMonitor, IStatelessActorBuilder, Type>((connectionStatusMonitor, builder, actorType) =>
             {
                 var actor = (IEventStoreStatelessActor)applicationBuilder.ApplicationServices.GetService(actorType);

                 var healthCheckService = applicationBuilder.ApplicationServices.GetService<IDynamicHealthCheckProvider>();
                 healthCheckService.AddHealthCheck(new HealthCheckRegistration(actor.Id, actor, HealthStatus.Unhealthy, null));

                 var loggerFactory = applicationBuilder.ApplicationServices.GetService<ILoggerFactory>();

                 foreach (var getStream in builder.GetStreamFactories())
                 {
                     var eventStoreStream = getStream(connectionStatusMonitor, loggerFactory);

                     actor.SubscribeToEventStream(eventStoreStream, closeUnderlyingSubscriptionOnDispose: true);
                 }

                 foreach (var busConfiguration in builder.GetBusFactories())
                 {
                     busConfiguration.factory(applicationBuilder.ApplicationServices, actor);
                 }

             });

            var connectionStatusMonitor = applicationBuilder.ApplicationServices.GetService<IConnectionStatusMonitor>();

            foreach (var (actorType, builder) in _world.StatelessActorBuilders)
            {
                registerStreamsAndBus(connectionStatusMonitor, builder, actorType);
            }

            foreach (var (actorType, builder) in _world.StatefulActorBuilders)
            {
                registerStreamsAndBus(connectionStatusMonitor, builder, actorType);
            }

            return applicationBuilder;
        }

        public static World AddWorld(this IServiceCollection services,
            string eventStoreUrl,
            ConnectionSettings connectionSettings,
            Action<IEventStoreRepositoryConfiguration> getEventStoreRepositoryConfiguration = null)
        {
            if (null != _world) throw new InvalidOperationException("A world already exist");

            var eventStoreConnection = EventStoreConnection.Create(connectionSettings, new Uri(eventStoreUrl));

            services.AddSingleton<IConnectionStatusMonitor, ConnectionStatusMonitor>();
            services.AddSingleton(eventStoreConnection);
            services.AddSingleton(connectionSettings);

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();

            getEventStoreRepositoryConfiguration?.Invoke(eventStoreRepositoryConfiguration);

            services.AddSingleton<IEventStoreRepositoryConfiguration>(eventStoreRepositoryConfiguration);

            services.AddTransient<IEventStoreRepository, EventStoreRepository>();

            return _world = new World(services);

        }

        public static World AddWorld(this IServiceCollection services,
            ClusterVNode clusterVNode,
            ConnectionSettings connectionSettings,
            Action<IEventStoreRepositoryConfiguration> getEventStoreRepositoryConfiguration = null)
        {
            if (null != _world) throw new InvalidOperationException("A world already exist");

            var eventStoreConnection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);

            services.AddSingleton<IConnectionStatusMonitor, ConnectionStatusMonitor>();
            services.AddSingleton(eventStoreConnection);
            services.AddSingleton(connectionSettings);

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();

            getEventStoreRepositoryConfiguration?.Invoke(eventStoreRepositoryConfiguration);

            services.AddSingleton<IEventStoreRepositoryConfiguration>(eventStoreRepositoryConfiguration);

            services.AddTransient<IEventStoreRepository, EventStoreRepository>();

            return _world = new World(services);

        }

    }
}
