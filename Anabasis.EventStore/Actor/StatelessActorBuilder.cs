using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Queue;
using Anabasis.EventStore.Repository;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Core;
using Lamar;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;

namespace Anabasis.EventStore.Actor
{
    public class StatelessActorBuilder<TActor, TRegistry>
      where TActor : IStatelessActor
      where TRegistry : ServiceRegistry, new()
    {

        private EventStoreRepository EventStoreRepository { get; set; }
        private UserCredentials UserCredentials { get; set; }
        private ConnectionStatusMonitor ConnectionMonitor { get; set; }
        private readonly List<IEventStoreQueue> _queuesToRegisterTo;

        private StatelessActorBuilder()
        {
            _queuesToRegisterTo = new List<IEventStoreQueue>();
        }

        public TActor Build()
        {
            var container = new Container(configuration =>
            {
                configuration.For<IEventStoreRepository>().Use(EventStoreRepository);
                configuration.For<IConnectionStatusMonitor>().Use(ConnectionMonitor);
                configuration.IncludeRegistry<TRegistry>();
            });

            var actor = container.GetInstance<TActor>();

            foreach (var queue in _queuesToRegisterTo)
            {
                actor.SubscribeTo(queue, closeSubscriptionOnDispose: true);
            }

            return actor;

        }

        public static StatelessActorBuilder<TActor, TRegistry> Create(
            string eventStoreUrl,
            UserCredentials userCredentials,
            ConnectionSettings connectionSettings,
            Action<IEventStoreRepositoryConfiguration> getEventStoreRepositoryConfiguration = null,
            IEventTypeProvider eventTypeProvider = null,
            ILoggerFactory loggerFactory = null)
        {

            var connection = EventStoreConnection.Create(connectionSettings, new Uri(eventStoreUrl));

            var builder = new StatelessActorBuilder<TActor, TRegistry>
            {
                UserCredentials = userCredentials,
                ConnectionMonitor = new ConnectionStatusMonitor(connection, loggerFactory)
            };

            var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration(userCredentials);

            getEventStoreRepositoryConfiguration?.Invoke(eventStoreRepositoryConfiguration);

            builder.EventStoreRepository = new EventStoreRepository(
              eventStoreRepositoryConfiguration,
              connection,
              builder.ConnectionMonitor,
              eventProvider,
              loggerFactory);

            return builder;

        }

        public static StatelessActorBuilder<TActor, TRegistry> Create(ClusterVNode clusterVNode,
          UserCredentials userCredentials,
          ConnectionSettings connectionSettings,
          Action<IEventStoreRepositoryConfiguration> getEventStoreRepositoryConfiguration = null,
          IEventTypeProvider eventTypeProvider = null,
          ILoggerFactory loggerfactory = null)
        {

            var builder = new StatelessActorBuilder<TActor, TRegistry>();

            var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);

            builder.UserCredentials = userCredentials;
            builder.ConnectionMonitor = new ConnectionStatusMonitor(connection, loggerfactory);

            var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration(userCredentials);

            getEventStoreRepositoryConfiguration?.Invoke(eventStoreRepositoryConfiguration);

            builder.EventStoreRepository = new EventStoreRepository(
              eventStoreRepositoryConfiguration,
              connection,
              builder.ConnectionMonitor,
              eventProvider,
              loggerfactory);

            return builder;

        }

        public StatelessActorBuilder<TActor, TRegistry> WithSubscribeToAllQueue(IEventTypeProvider eventTypeProvider = null)
        {
            var volatileEventStoreQueueConfiguration = new SubscribeFromEndEventStoreQueueConfiguration(UserCredentials);

            var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

            var volatileEventStoreQueue = new SubscribeFromEndEventStoreQueue(
              ConnectionMonitor,
              volatileEventStoreQueueConfiguration,
              eventProvider);

            _queuesToRegisterTo.Add(volatileEventStoreQueue);

            return this;
        }

        public StatelessActorBuilder<TActor, TRegistry> WithSubscribeToOneStreamQueue(string streamId, IEventTypeProvider eventTypeProvider = null)
        {
            var volatileEventStoreQueueConfiguration = new SubscribeFromEndEventStoreQueueConfiguration(UserCredentials);

            var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

            var volatileEventStoreQueue = new SubscribeFromEndEventStoreQueue(
              ConnectionMonitor,
              volatileEventStoreQueueConfiguration,
              eventProvider);

            _queuesToRegisterTo.Add(volatileEventStoreQueue);

            return this;
        }

        public StatelessActorBuilder<TActor, TRegistry> WithPersistentSubscriptionQueue(string streamId, string groupId)
        {
            var persistentEventStoreQueueConfiguration = new PersistentSubscriptionEventStoreQueueConfiguration(streamId, groupId, UserCredentials);

            var eventProvider = new ConsumerBasedEventProvider<TActor>();

            var persistentSubscriptionEventStoreQueue = new PersistentSubscriptionEventStoreQueue(
              ConnectionMonitor,
              persistentEventStoreQueueConfiguration,
              eventProvider);

            _queuesToRegisterTo.Add(persistentSubscriptionEventStoreQueue);

            return this;
        }

    }
}
