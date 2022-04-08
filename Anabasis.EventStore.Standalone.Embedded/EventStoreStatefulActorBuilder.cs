using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using EventStore.ClientAPI;
using Lamar;
using Microsoft.Extensions.Logging;
using System;
using Anabasis.Common;
using EventStore.Core;
using EventStore.ClientAPI.Embedded;

namespace Anabasis.EventStore.Standalone.Embedded
{
    public static class EventStoreEmbeddedStatefulActorBuilder<TActor, TAggregate, TRegistry>
        where TActor : IStatefulActor<TAggregate>
        where TAggregate : class, IAggregate, new()
        where TRegistry : ServiceRegistry, new()
    {

        public static EventStoreStatefulActorBuilder<TActor, TAggregate, TRegistry> Create(
          ClusterVNode clusterVNode,
          ConnectionSettings connectionSettings,
          IActorConfiguration actorConfiguration,
          ILoggerFactory? loggerFactory = null,
          Action<IEventStoreRepositoryConfiguration>? eventStoreRepositoryConfigurationBuilder = null)
        {

            var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);
            var connectionMonitor = new EventStoreConnectionStatusMonitor(connection, loggerFactory);

            loggerFactory ??= new DummyLoggerFactory();

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();
            eventStoreRepositoryConfigurationBuilder?.Invoke(eventStoreRepositoryConfiguration);

            var eventStoreRepository = new EventStoreAggregateRepository(
              eventStoreRepositoryConfiguration,
              connection,
              connectionMonitor,
              loggerFactory);

            var builder = new EventStoreStatefulActorBuilder<TActor, TAggregate, TRegistry>(actorConfiguration, eventStoreRepository, connectionMonitor, loggerFactory);

            return builder;

        }


    }

}
