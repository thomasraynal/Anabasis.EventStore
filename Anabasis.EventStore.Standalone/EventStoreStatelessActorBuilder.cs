using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using EventStore.ClientAPI;
using Lamar;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Anabasis.Common;

namespace Anabasis.EventStore.Standalone
{
    public class EventStoreStatelessActorBuilder<TActor, TRegistry>
      where TActor : IActor
      where TRegistry : ServiceRegistry, new()
    {

        public IEventStoreRepository EventStoreRepository { get; private set; }
        public ILoggerFactory LoggerFactory { get; private set; }
        public IConnectionStatusMonitor<IEventStoreConnection> ConnectionMonitor { get; private set; }
        public IActorConfiguration ActorConfiguration { get; private set; }

        private readonly Dictionary<Type,Action<Container,IActor>> _busToRegisterTo;

        public EventStoreStatelessActorBuilder(
            IActorConfiguration actorConfiguration,
            IEventStoreRepository eventStoreRepository,
            IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor,
            ILoggerFactory loggerFactory
            )
        {
            EventStoreRepository = eventStoreRepository;
            LoggerFactory = loggerFactory;
            ActorConfiguration = actorConfiguration;
            ConnectionMonitor = connectionMonitor;

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

            foreach (var busRegistration in _busToRegisterTo)
            {
                var bus = (IBus)container.GetInstance(busRegistration.Key);

                actor.ConnectTo(bus).Wait();

                var onBusRegistration = busRegistration.Value;

                onBusRegistration(container, actor);

            }

            actor.OnInitialized().Wait();

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

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();

            getEventStoreRepositoryConfiguration?.Invoke(eventStoreRepositoryConfiguration);

            var connectionMonitor = new EventStoreConnectionStatusMonitor(connection, loggerFactory);

            var eventStoreRepository = new EventStoreRepository(
              eventStoreRepositoryConfiguration,
              connection,
              connectionMonitor,
              loggerFactory);

            var builder = new EventStoreStatelessActorBuilder<TActor, TRegistry>(actorConfiguration, eventStoreRepository, connectionMonitor, loggerFactory);       

            return builder;

        }

        public static EventStoreStatelessActorBuilder<TActor, TRegistry> Create(string eventStoreConnectionString,
             ConnectionSettingsBuilder connectionSettingsBuilder,
             IActorConfiguration actorConfiguration,
             ILoggerFactory loggerFactory = null,
             Action<IEventStoreRepositoryConfiguration> getEventStoreRepositoryConfiguration = null)
        {

            var connection = EventStoreConnection.Create(eventStoreConnectionString, connectionSettingsBuilder);

            loggerFactory ??= new DummyLoggerFactory();

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();

            getEventStoreRepositoryConfiguration?.Invoke(eventStoreRepositoryConfiguration);

            var connectionMonitor = new EventStoreConnectionStatusMonitor(connection, loggerFactory);

            var eventStoreRepository = new EventStoreRepository(
              eventStoreRepositoryConfiguration,
              connection,
              connectionMonitor,
              loggerFactory);

            var builder = new EventStoreStatelessActorBuilder<TActor, TRegistry>(actorConfiguration, eventStoreRepository, connectionMonitor, loggerFactory);

            return builder;

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
