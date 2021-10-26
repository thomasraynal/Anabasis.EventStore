using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Queue;
using Anabasis.EventStore.Repository;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.Core;
using Lamar;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Anabasis.EventStore.Standalone
{
    public class StatelessActorBuilder<TActor, TRegistry>
      where TActor : IStatelessActor
      where TRegistry : ServiceRegistry, new()
    {

        private EventStoreRepository EventStoreRepository { get; set; }
        private ILoggerFactory LoggerFactory { get;  set; }
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
                configuration.For<ILoggerFactory>().Use(LoggerFactory);
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
            ConnectionSettings connectionSettings,
            ILoggerFactory loggerFactory = null,
            Action<IEventStoreRepositoryConfiguration> getEventStoreRepositoryConfiguration = null)
        {

            var connection = EventStoreConnection.Create(connectionSettings, new Uri(eventStoreUrl));

            loggerFactory ??= new DummyLoggerFactory();

            var builder = new StatelessActorBuilder<TActor, TRegistry>
            {
                ConnectionMonitor = new ConnectionStatusMonitor(connection, loggerFactory),
                LoggerFactory = loggerFactory
            };

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();

            getEventStoreRepositoryConfiguration?.Invoke(eventStoreRepositoryConfiguration);

            builder.EventStoreRepository = new EventStoreRepository(
              eventStoreRepositoryConfiguration,
              connection,
              builder.ConnectionMonitor,
              loggerFactory);

            return builder;

        }

        public static StatelessActorBuilder<TActor, TRegistry> Create(ClusterVNode clusterVNode,
            ConnectionSettings connectionSettings,
            ILoggerFactory loggerFactory = null,
            Action<IEventStoreRepositoryConfiguration> getEventStoreRepositoryConfiguration = null
            )
        {

            var builder = new StatelessActorBuilder<TActor, TRegistry>();

            var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);

            builder.LoggerFactory = loggerFactory ?? new DummyLoggerFactory();
            builder.ConnectionMonitor = new ConnectionStatusMonitor(connection, builder.LoggerFactory);

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();

            getEventStoreRepositoryConfiguration?.Invoke(eventStoreRepositoryConfiguration);

            builder.EventStoreRepository = new EventStoreRepository(
                eventStoreRepositoryConfiguration,
                connection,
                builder.ConnectionMonitor,
                builder.LoggerFactory);

            return builder;

        }

        public StatelessActorBuilder<TActor, TRegistry> WithSubscribeFromEndToAllQueue(IEventTypeProvider eventTypeProvider = null)
        {
            var subscribeFromEndEventStoreQueueConfiguration = new SubscribeFromEndEventStoreQueueConfiguration();

            var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

            var subscribeFromEndEventStoreQueue = new SubscribeFromEndEventStoreQueue(
              ConnectionMonitor,
              subscribeFromEndEventStoreQueueConfiguration,
              eventProvider,
              LoggerFactory);

            _queuesToRegisterTo.Add(subscribeFromEndEventStoreQueue);

            return this;
        }

        public StatelessActorBuilder<TActor, TRegistry> WithSubscribeFromEndToOneStreamQueue(string streamId,  IEventTypeProvider eventTypeProvider = null)
        {
            var subscribeFromEndToOneStreamEventStoreQueueConfiguration = new SubscribeToOneStreamFromStartOrLaterEventStoreQueueConfiguration(streamId);

            var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

            var subscribeFromEndToOneStreamEventStoreQueue = new SubscribeFromEndToOneStreamEventStoreQueue(
              ConnectionMonitor,
              subscribeFromEndToOneStreamEventStoreQueueConfiguration,
              eventProvider,
              LoggerFactory);

            _queuesToRegisterTo.Add(subscribeFromEndToOneStreamEventStoreQueue);

            return this;
        }

        public StatelessActorBuilder<TActor, TRegistry> WithPersistentSubscriptionQueue(string streamId, string groupId)
        {
            var persistentEventStoreQueueConfiguration = new PersistentSubscriptionEventStoreQueueConfiguration(streamId, groupId);

            var eventProvider = new ConsumerBasedEventProvider<TActor>();

            var persistentSubscriptionEventStoreQueue = new PersistentSubscriptionEventStoreQueue(
              ConnectionMonitor,
              persistentEventStoreQueueConfiguration,
              eventProvider,
              LoggerFactory);

            _queuesToRegisterTo.Add(persistentSubscriptionEventStoreQueue);

            return this;
        }

    }
}
