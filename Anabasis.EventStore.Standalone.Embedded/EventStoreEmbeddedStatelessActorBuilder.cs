using Anabasis.Common;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.Core;
using Lamar;
using Microsoft.Extensions.Logging;
using System;

namespace Anabasis.EventStore.Standalone.Embedded
{
    public static class EventStoreEmbeddedStatelessActorBuilder<TActor, TRegistry>
        where TActor : IActor
        where TRegistry : ServiceRegistry, new()
    {

        public static EventStoreStatelessActorBuilder<TActor, TRegistry> Create(
            ClusterVNode clusterVNode,
            ConnectionSettings connectionSettings,
            IActorConfiguration actorConfiguration,
            ILoggerFactory? loggerFactory = null,
            Action<IEventStoreRepositoryConfiguration>? getEventStoreRepositoryConfiguration = null)

        {

            var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);

            loggerFactory ??= new DummyLoggerFactory();

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();
            getEventStoreRepositoryConfiguration?.Invoke(eventStoreRepositoryConfiguration);

            var connectionStatusMonitor = new EventStoreConnectionStatusMonitor(connection, loggerFactory);

            var eventStoreRepository = new EventStoreRepository(
                eventStoreRepositoryConfiguration,
                connection,
                connectionStatusMonitor,
                loggerFactory);

            var eventStoreStatelessActorBuilder = new EventStoreStatelessActorBuilder<TActor, TRegistry>(actorConfiguration, eventStoreRepository, connectionStatusMonitor, loggerFactory);

            return eventStoreStatelessActorBuilder;

        }

    }
}
