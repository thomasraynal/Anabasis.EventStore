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
      where TActor : IEventStoreStatelessActor
      where TRegistry : ServiceRegistry, new()
    {

        private IEventStoreRepository EventStoreRepository { get; set; }
        private ILoggerFactory LoggerFactory { get;  set; }
        private IConnectionStatusMonitor ConnectionMonitor { get; set; }
        private IActorConfiguration ActorConfiguration { get; set; }

        private readonly List<IEventStoreStream> _streamsToRegisterTo;

        private StatelessActorBuilder()
        {
            _streamsToRegisterTo = new List<IEventStoreStream>();
        }

        public TActor Build()
        {
            var container = new Container(configuration =>
            {
                configuration.For<IActorConfiguration>().Use(ActorConfiguration);
                configuration.For<ILoggerFactory>().Use(LoggerFactory);
                configuration.For<IEventStoreRepository>().Use(EventStoreRepository);
                configuration.For<IConnectionStatusMonitor>().Use(ConnectionMonitor);
                configuration.IncludeRegistry<TRegistry>();
            });

            var actor = container.GetInstance<TActor>();

            foreach (var stream in _streamsToRegisterTo)
            {
                actor.SubscribeToEventStream(stream, closeUnderlyingSubscriptionOnDispose: true);
            }

            return actor;

        }

        public static StatelessActorBuilder<TActor, TRegistry> Create(
            string eventStoreUrl,
            ConnectionSettings connectionSettings,
            IActorConfiguration actorConfiguration,
            ILoggerFactory loggerFactory = null,
            Action<IEventStoreRepositoryConfiguration> getEventStoreRepositoryConfiguration = null)
        {

            var connection = EventStoreConnection.Create(connectionSettings, new Uri(eventStoreUrl));

            loggerFactory ??= new DummyLoggerFactory();

            var builder = new StatelessActorBuilder<TActor, TRegistry>
            {
                ConnectionMonitor = new ConnectionStatusMonitor(connection, loggerFactory),
                LoggerFactory = loggerFactory,
                ActorConfiguration = actorConfiguration
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
            IActorConfiguration actorConfiguration,
            ILoggerFactory loggerFactory = null,
            Action<IEventStoreRepositoryConfiguration> getEventStoreRepositoryConfiguration = null
            )
        {

            var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);

            loggerFactory ??= new DummyLoggerFactory();

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();
            getEventStoreRepositoryConfiguration?.Invoke(eventStoreRepositoryConfiguration);

            var connectionStatusMonitor = new ConnectionStatusMonitor(connection, loggerFactory);

            var eventStoreRepository = new EventStoreRepository(
                eventStoreRepositoryConfiguration,
                connection,
                connectionStatusMonitor,
                loggerFactory);

            var builder = new StatelessActorBuilder<TActor, TRegistry>()
            {
                ActorConfiguration = actorConfiguration,
                LoggerFactory = loggerFactory,
                ConnectionMonitor = connectionStatusMonitor,
                EventStoreRepository = eventStoreRepository
            };

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
