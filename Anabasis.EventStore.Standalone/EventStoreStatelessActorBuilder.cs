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
    public class EventStoreStatelessActorBuilder<TActor, TRegistry>
      where TActor : IEventStoreStatelessActor
      where TRegistry : ServiceRegistry, new()
    {

        private IEventStoreRepository EventStoreRepository { get; set; }
        private ILoggerFactory LoggerFactory { get;  set; }
        private IConnectionStatusMonitor<IEventStoreConnection> ConnectionMonitor { get; set; }
        private IActorConfiguration ActorConfiguration { get; set; }

        private readonly List<IEventStoreStream> _streamsToRegisterTo;
        private readonly Dictionary<Type,Action<Container,IActor>> _busToRegisterTo;

        private EventStoreStatelessActorBuilder()
        {
            _streamsToRegisterTo = new List<IEventStoreStream>();
            _busToRegisterTo = new Dictionary<Type, Action<Container, IActor>>();
        }

        public TActor Build()
        {
            var container = new Container(configuration =>
            {
                configuration.For<IActorConfiguration>().Use(ActorConfiguration);
                configuration.For<ILoggerFactory>().Use(LoggerFactory);
                configuration.For<IEventStoreRepository>().Use(EventStoreRepository);
                configuration.For<IConnectionStatusMonitor<IEventStoreConnection>>().Use(ConnectionMonitor);
                configuration.IncludeRegistry<TRegistry>();
            });

            var actor = container.GetInstance<TActor>();

            foreach (var stream in _streamsToRegisterTo)
            {
                actor.SubscribeToEventStream(stream, closeSubscriptionOnDispose: true);
            }

            foreach (var busRegistration in _busToRegisterTo)
            {
                var bus = (IBus)container.GetInstance(busRegistration.Key);

                bus.Initialize().Wait();
                actor.ConnectTo(bus).Wait();

                var onBusRegistration = busRegistration.Value;

                onBusRegistration(container, actor);

            }

            return actor;

        }

        public static EventStoreStatelessActorBuilder<TActor, TRegistry> Create(
            Uri eventStoreUrl,
            ConnectionSettings connectionSettings,
            IActorConfiguration actorConfiguration,
            ILoggerFactory loggerFactory = null,
            Action<IEventStoreRepositoryConfiguration> getEventStoreRepositoryConfiguration = null)
        {

            var connection = EventStoreConnection.Create(connectionSettings, eventStoreUrl);

            loggerFactory ??= new DummyLoggerFactory();

            var builder = new EventStoreStatelessActorBuilder<TActor, TRegistry>
            {
                ConnectionMonitor = new EventStoreConnectionStatusMonitor(connection, loggerFactory),
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

        public static EventStoreStatelessActorBuilder<TActor, TRegistry> Create(string eventStoreConnectionString,
             ConnectionSettingsBuilder connectionSettingsBuilder,
             IActorConfiguration actorConfiguration,
             ILoggerFactory loggerFactory = null,
             Action<IEventStoreRepositoryConfiguration> getEventStoreRepositoryConfiguration = null)
        {

            var eventStoreConnection = EventStoreConnection.Create(eventStoreConnectionString, connectionSettingsBuilder);

            loggerFactory ??= new DummyLoggerFactory();

            var builder = new EventStoreStatelessActorBuilder<TActor, TRegistry>
            {
                ConnectionMonitor = new EventStoreConnectionStatusMonitor(eventStoreConnection, loggerFactory),
                LoggerFactory = loggerFactory,
                ActorConfiguration = actorConfiguration
            };

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();

            getEventStoreRepositoryConfiguration?.Invoke(eventStoreRepositoryConfiguration);

            builder.EventStoreRepository = new EventStoreRepository(
              eventStoreRepositoryConfiguration,
              eventStoreConnection,
              builder.ConnectionMonitor,
              loggerFactory);

            return builder;

        }

        public static EventStoreStatelessActorBuilder<TActor, TRegistry> Create(ClusterVNode clusterVNode,
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

            var connectionStatusMonitor = new EventStoreConnectionStatusMonitor(connection, loggerFactory);

            var eventStoreRepository = new EventStoreRepository(
                eventStoreRepositoryConfiguration,
                connection,
                connectionStatusMonitor,
                loggerFactory);

            var builder = new EventStoreStatelessActorBuilder<TActor, TRegistry>()
            {
                ActorConfiguration = actorConfiguration,
                LoggerFactory = loggerFactory,
                ConnectionMonitor = connectionStatusMonitor,
                EventStoreRepository = eventStoreRepository
            };

            return builder;

        }

        public EventStoreStatelessActorBuilder<TActor, TRegistry> WithSubscribeFromEndToAllStream(IEventTypeProvider eventTypeProvider = null)
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

        public EventStoreStatelessActorBuilder<TActor, TRegistry> WithSubscribeFromEndToOneStream(string streamId,  IEventTypeProvider eventTypeProvider = null)
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

        public EventStoreStatelessActorBuilder<TActor, TRegistry> WithPersistentSubscriptionStream(string streamId, string groupId)
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

        public EventStoreStatelessActorBuilder<TActor, TRegistry> WithBus<TBus>(Action<TActor, TBus> onStartup =null) where TBus : IBus
        {
            var busType = typeof(TBus);

            onStartup ??= new Action<TActor, TBus>((actor, bus) => { });

            if (_busToRegisterTo.ContainsKey(busType))
                throw new InvalidOperationException($"ActorBuilder already has a reference to a bus of type {busType}");

            var onRegistration = new Action<Container, IActor>((container, actor) =>
            {
                var bus = container.GetInstance<TBus>();

                if (null == bus)
                    throw new InvalidOperationException($"No bus of type {busType} has been registered");

                onStartup((TActor)actor, bus);

            });

            _busToRegisterTo.Add(busType, onRegistration);

            return this;
        }

    }
}
