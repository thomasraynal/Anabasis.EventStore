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

namespace Anabasis.EventStore
{

    public static class ServiceBuilderActorExtensions
    {

        private static World _world;

        public static IApplicationBuilder UseWorld(this IApplicationBuilder applicationBuilder)
        {
            var registerStreams = new Action<IConnectionStatusMonitor, IStatelessActorBuilder, Type>((connectionStatusMonitor, builder, actorType) =>
             {
                 var actor = (IEventStoreStatelessActor)applicationBuilder.ApplicationServices.GetService(actorType);
                 var loggerFactory = applicationBuilder.ApplicationServices.GetService<ILoggerFactory>();

                 foreach (var getStream in builder.GetStreamFactories())
                 {
                     actor.SubscribeToEventStream(getStream(connectionStatusMonitor, loggerFactory), closeUnderlyingSubscriptionOnDispose: true);
                 } 

             });

            var connectionStatusMonitor = applicationBuilder.ApplicationServices.GetService<IConnectionStatusMonitor>();

            foreach (var (actorType, builder) in _world.StatelessActorBuilders)
            {
                registerStreams(connectionStatusMonitor, builder, actorType);
            }

            foreach (var (actorType, builder) in _world.StatefulActorBuilders)
            {
                registerStreams(connectionStatusMonitor, builder, actorType);
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
