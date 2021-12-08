using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Stream;
using Anabasis.EventStore.Repository;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.Core;
using Lamar;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Anabasis.Common;

namespace Anabasis.EventStore.Standalone
{
    public class StatelessActorBuilder<TActor, TRegistry>
      where TActor : IStatelessActor
      where TRegistry : ServiceRegistry, new()
    {

        private EventStoreRepository EventStoreRepository { get; set; }
        private ILoggerFactory LoggerFactory { get;  set; }
        private ConnectionStatusMonitor ConnectionMonitor { get; set; }
        private readonly List<IEventStream> _streamsToRegisterTo;

        private StatelessActorBuilder()
        {
            _streamsToRegisterTo = new List<IEventStream>();
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

            foreach (var stream in _streamsToRegisterTo)
            {
                actor.SubscribeTo(stream, closeSubscriptionOnDispose: true);
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

        public StatelessActorBuilder<TActor, TRegistry> WithSubscribeFromEndToAllStream(IEventTypeProvider eventTypeProvider = null)
        {
            var subscribeFromEndEventStoreStreamConfiguration = new SubscribeFromEndEventStoreStreamConfiguration();

            var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

            var subscribeFromEndEventStoreStream = new SubscribeFromEndEventStoreStream(
              ConnectionMonitor,
              subscribeFromEndEventStoreStreamConfiguration,
              eventProvider,
              LoggerFactory);

            _streamsToRegisterTo.Add(subscribeFromEndEventStoreStream);

            return this;
        }

        public StatelessActorBuilder<TActor, TRegistry> WithSubscribeFromEndToOneStreamStream(string streamId,  IEventTypeProvider eventTypeProvider = null)
        {
            var subscribeFromEndToOneStreamEventStoreStreamConfiguration = new SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration(streamId);

            var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

            var subscribeFromEndToOneStreamEventStoreStream = new SubscribeFromEndToOneStreamEventStoreStream(
              ConnectionMonitor,
              subscribeFromEndToOneStreamEventStoreStreamConfiguration,
              eventProvider,
              LoggerFactory);

            _streamsToRegisterTo.Add(subscribeFromEndToOneStreamEventStoreStream);

            return this;
        }

        public StatelessActorBuilder<TActor, TRegistry> WithPersistentSubscriptionStream(string streamId, string groupId)
        {
            var persistentEventStoreStreamConfiguration = new PersistentSubscriptionEventStoreStreamConfiguration(streamId, groupId);

            var eventProvider = new ConsumerBasedEventProvider<TActor>();

            var persistentSubscriptionEventStoreStream = new PersistentSubscriptionEventStoreStream(
              ConnectionMonitor,
              persistentEventStoreStreamConfiguration,
              eventProvider,
              LoggerFactory);

            _streamsToRegisterTo.Add(persistentSubscriptionEventStoreStream);

            return this;
        }

    }
}
