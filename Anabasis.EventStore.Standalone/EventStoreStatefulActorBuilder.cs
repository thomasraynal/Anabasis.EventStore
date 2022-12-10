using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Snapshot;
using EventStore.ClientAPI;
using Lamar;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using Anabasis.Common;

namespace Anabasis.EventStore.Standalone
{
    public class EventStoreStatefulActorBuilder<TActor, TAggregateCacheConfiguration, TAggregate, TRegistry>
      where TActor : IStatefulActor<TAggregate, TAggregateCacheConfiguration>
      where TAggregateCacheConfiguration : class, IAggregateCacheConfiguration
      where TAggregate : class, IAggregate, new()
      where TRegistry : ServiceRegistry, new()
    {
        public IEventStoreAggregateRepository EventStoreRepository { get; private set; }
        public ILoggerFactory? LoggerFactory { get; private set; }
        public IConnectionStatusMonitor<IEventStoreConnection> ConnectionMonitor { get; private set; }
        public IActorConfiguration ActorConfiguration { get; private set; }
        public IEventTypeProvider? EventTypeProvider { get; private set; }
        public TAggregateCacheConfiguration AggregateCacheConfiguration { get; private set; }
        public ISnapshotStore<TAggregate>? SnapshotStore { get; private set; }
        public ISnapshotStrategy? SnapshotStrategy { get; private set; }


        private readonly Dictionary<Type, Action<Container, IAnabasisActor>> _busToRegisterTo;

        public EventStoreStatefulActorBuilder(IActorConfiguration actorConfiguration,
            IEventStoreAggregateRepository eventStoreRepository,
            TAggregateCacheConfiguration aggregateCacheConfiguration,
            IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor,
            IEventTypeProvider? eventTypeProvider = null,
            ISnapshotStore<TAggregate>? snapshotStore = null,
            ISnapshotStrategy? snapshotStrategy = null,
            ILoggerFactory? loggerFactory = null)
        {
            ActorConfiguration = actorConfiguration;
            AggregateCacheConfiguration = aggregateCacheConfiguration;
            EventStoreRepository = eventStoreRepository;
            LoggerFactory = loggerFactory;
            ConnectionMonitor = connectionMonitor;
            EventTypeProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TAggregate, TActor>();
            SnapshotStrategy = snapshotStrategy;
            SnapshotStore = snapshotStore;

            _busToRegisterTo = new Dictionary<Type, Action<Container, IAnabasisActor>>();
        }

        public TActor Build()
        {

            var container = new Container(configuration =>
            {
                configuration.For<IActorConfiguration>().Use(ActorConfiguration);
                if (null != LoggerFactory) configuration.For<ILoggerFactory>().Use(LoggerFactory);
                configuration.For<IEventTypeProvider>().Use(EventTypeProvider);
                configuration.For<TAggregateCacheConfiguration>().Use(AggregateCacheConfiguration);
                if (null != SnapshotStore) configuration.For<ISnapshotStore<TAggregate>>().Use(SnapshotStore);
                if (null != SnapshotStrategy) configuration.For<ISnapshotStrategy>().Use(SnapshotStrategy);
                configuration.For<IEventStoreAggregateRepository>().Use(EventStoreRepository);
                configuration.For<IEventStoreRepository>().Use(EventStoreRepository);
                configuration.For<IConnectionStatusMonitor<IEventStoreConnection>>().Use(ConnectionMonitor);
                configuration.IncludeRegistry<TRegistry>();

            });

            var actor = container.GetInstance<TActor>();

            foreach (var busRegistration in _busToRegisterTo)
            {
                var bus = (IBus)container.GetInstance(busRegistration.Key);

                if (null == bus)
                    throw new InvalidOperationException($"No bus of type {busRegistration.Key} has been registered");

                actor.ConnectTo(bus).Wait();

                var onBusRegistration = busRegistration.Value;

                onBusRegistration(container, actor);

            }

            actor.OnInitialized().Wait();

            return actor;

        }

        public static EventStoreStatefulActorBuilder<TActor, TAggregateCacheConfiguration, TAggregate, TRegistry> Create(
            Uri eventStoreUrl,
            ConnectionSettings connectionSettings,
            TAggregateCacheConfiguration aggregateCacheConfiguration,
            IActorConfiguration actorConfiguration,
            IEventTypeProvider? eventTypeProvider = null,
            ILoggerFactory? loggerFactory = null,
            ISnapshotStore<TAggregate>? snapshotStore = null,
            ISnapshotStrategy? snapshotStrategy = null,
            Action<IEventStoreRepositoryConfiguration>? getEventStoreRepositoryConfigurationBuilder = null)
        {
            var eventStoreConnection = EventStoreConnection.Create(connectionSettings, eventStoreUrl);

            return CreateInternal(actorConfiguration, 
                aggregateCacheConfiguration, 
                eventStoreConnection, 
                eventTypeProvider, 
                loggerFactory,
                snapshotStore,
                snapshotStrategy,
                getEventStoreRepositoryConfigurationBuilder);

        }

        public static EventStoreStatefulActorBuilder<TActor, TAggregateCacheConfiguration, TAggregate, TRegistry> Create(
             string eventStoreConnectionString,
             ConnectionSettingsBuilder connectionSettingsBuilder,
             TAggregateCacheConfiguration aggregateCacheConfiguration,
             IActorConfiguration actorConfiguration,
             IEventTypeProvider? eventTypeProvider = null,
             ILoggerFactory? loggerFactory = null,
             ISnapshotStore<TAggregate>? snapshotStore = null,
             ISnapshotStrategy? snapshotStrategy = null,
             Action<IEventStoreRepositoryConfiguration>? eventStoreRepositoryConfigurationBuilder = null)
        {

            var eventStoreConnection = EventStoreConnection.Create(eventStoreConnectionString, connectionSettingsBuilder);

            return CreateInternal(actorConfiguration, aggregateCacheConfiguration, eventStoreConnection, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy, eventStoreRepositoryConfigurationBuilder);

        }

        private static EventStoreStatefulActorBuilder<TActor, TAggregateCacheConfiguration, TAggregate, TRegistry> CreateInternal(
          IActorConfiguration actorConfiguration,
          TAggregateCacheConfiguration aggregateCacheConfiguration,
          IEventStoreConnection eventStoreConnection,
          IEventTypeProvider? eventTypeProvider = null,
          ILoggerFactory? loggerFactory = null,
          ISnapshotStore<TAggregate>? snapshotStore = null,
          ISnapshotStrategy? snapshotStrategy = null,
          Action<IEventStoreRepositoryConfiguration>? eventStoreRepositoryConfigurationBuilder = null)
        {

            loggerFactory ??= new DummyLoggerFactory();

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();

            eventStoreRepositoryConfigurationBuilder?.Invoke(eventStoreRepositoryConfiguration);

            var connectionMonitor = new EventStoreConnectionStatusMonitor(eventStoreConnection, loggerFactory);

            var eventStoreRepository = new EventStoreAggregateRepository(
              eventStoreRepositoryConfiguration,
              eventStoreConnection,
              connectionMonitor,
              loggerFactory);

            var builder = new EventStoreStatefulActorBuilder<TActor, TAggregateCacheConfiguration, TAggregate, TRegistry>(
                actorConfiguration,
                eventStoreRepository,
                aggregateCacheConfiguration,
                connectionMonitor,
                eventTypeProvider,
                snapshotStore,
                snapshotStrategy,
                loggerFactory);

            return builder;

        }

        public EventStoreStatefulActorBuilder<TActor, TAggregateCacheConfiguration, TAggregate, TRegistry> WithBus<TBus>(Action<TActor, TBus>? onStartup = null) where TBus : IBus
        {
            var busType = typeof(TBus);

            onStartup ??= new Action<TActor, TBus>((actor, bus) => { });

            if (_busToRegisterTo.ContainsKey(busType))
                throw new InvalidOperationException($"ActorBuilder already has a reference to a bus of type {busType}");

            var onRegistration = new Action<Container, IAnabasisActor>((container, actor) =>
            {
                var bus = container.GetInstance<TBus>();

                onStartup((TActor)actor, bus);

            });

            _busToRegisterTo.Add(busType, onRegistration);

            return this;
        }
    }

}
