using EventStore.ClientAPI;
using Microsoft.Extensions.DependencyInjection;
using System;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Anabasis.Common.HealthChecks;
using Anabasis.Common;
using Anabasis.EventStore.AspNet.Builders;
using EventStore.Core;
using EventStore.ClientAPI.Embedded;

namespace Anabasis.EventStore.Embedded
{

    public static class ServiceBuilderEmbeddedActorExtensions
    {

  
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
