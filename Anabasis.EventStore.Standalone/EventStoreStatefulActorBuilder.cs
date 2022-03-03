using Anabasis.EventStore.Cache;
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
    public class EventStoreStatefulActorBuilder<TActor, TAggregate, TRegistry>
      where TActor : IStatefulActor<TAggregate>
      where TAggregate : class, IAggregate, new()
      where TRegistry : ServiceRegistry, new()
    {
        public IAggregateCache<TAggregate> EventStoreCache { get; private set; }
        public IEventStoreAggregateRepository EventStoreRepository { get; private set; }
        public ILoggerFactory LoggerFactory { get; private set; }
        public IConnectionStatusMonitor<IEventStoreConnection> ConnectionMonitor { get; private set; }
        public IActorConfiguration ActorConfiguration { get; private set; }

        private readonly Dictionary<Type, Action<Container, IActor>> _busToRegisterTo;

        public EventStoreStatefulActorBuilder(IActorConfiguration actorConfiguration,
            IEventStoreAggregateRepository eventStoreRepository,
            IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor,
            ILoggerFactory loggerFactory
            )
        {
            ActorConfiguration = actorConfiguration;
            EventStoreRepository = eventStoreRepository;
            LoggerFactory = loggerFactory;
            ConnectionMonitor = connectionMonitor;

            _busToRegisterTo = new Dictionary<Type, Action<Container, IActor>>();
        }

        public TActor Build()
        {
            if (null == EventStoreCache) throw new InvalidOperationException($"You must specify a cache for an StatefulActor." +
                $" Use the With* methods on the builder to choose the cache type.");

            var container = new Container(configuration =>
            {
                configuration.For<IActorConfiguration>().Use(ActorConfiguration);
                configuration.For<ILoggerFactory>().Use(LoggerFactory);
                configuration.For<IAggregateCache<TAggregate>>().Use(EventStoreCache);
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

        public static EventStoreStatefulActorBuilder<TActor, TAggregate, TRegistry> Create(Uri eventStoreUrl,
        ConnectionSettings connectionSettings,
        IActorConfiguration actorConfiguration,
        ILoggerFactory loggerFactory = null,
        Action<IEventStoreRepositoryConfiguration> getEventStoreRepositoryConfigurationBuilder = null)
        {
            var connection = EventStoreConnection.Create(connectionSettings, eventStoreUrl);

            return CreateInternal(actorConfiguration, connection, loggerFactory, getEventStoreRepositoryConfigurationBuilder);

        }
        
         public static EventStoreStatefulActorBuilder<TActor, TAggregate, TRegistry> Create(string eventStoreConnectionString,
          ConnectionSettingsBuilder connectionSettingsBuilder,
          IActorConfiguration actorConfiguration,
          ILoggerFactory loggerFactory = null,
          Action<IEventStoreRepositoryConfiguration> eventStoreRepositoryConfigurationBuilder = null)
        {

            var eventStoreConnection = EventStoreConnection.Create(eventStoreConnectionString, connectionSettingsBuilder);

            return CreateInternal(actorConfiguration, eventStoreConnection, loggerFactory, eventStoreRepositoryConfigurationBuilder);

        }

        private static EventStoreStatefulActorBuilder<TActor, TAggregate, TRegistry> CreateInternal(
          IActorConfiguration actorConfiguration,
          IEventStoreConnection eventStoreConnection,
          ILoggerFactory loggerFactory = null,
          Action<IEventStoreRepositoryConfiguration> eventStoreRepositoryConfigurationBuilder = null)
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

            var builder = new EventStoreStatefulActorBuilder<TActor, TAggregate, TRegistry>(actorConfiguration, eventStoreRepository, connectionMonitor, loggerFactory);

            return builder;

        }

        public EventStoreStatefulActorBuilder<TActor, TAggregate, TRegistry> WithReadAllFromStartCache(
          IEventTypeProvider<TAggregate> eventTypeProvider,
          Action<AllStreamsCatchupCacheConfiguration<TAggregate>> getCatchupEventStoreCacheConfigurationBuilder = null,
          ISnapshotStore<TAggregate> snapshotStore = null,
          ISnapshotStrategy snapshotStrategy = null)
        {
            if (null != EventStoreCache) throw new InvalidOperationException($"A cache has already been set => {EventStoreCache.GetType()}");

            var catchupEventStoreCacheConfiguration = new AllStreamsCatchupCacheConfiguration<TAggregate>(Position.Start);

            getCatchupEventStoreCacheConfigurationBuilder?.Invoke(catchupEventStoreCacheConfiguration);

            EventStoreCache = new AllStreamsCatchupCache<TAggregate>(ConnectionMonitor, catchupEventStoreCacheConfiguration, eventTypeProvider, LoggerFactory, snapshotStore, snapshotStrategy);

            return this;
        }

        public EventStoreStatefulActorBuilder<TActor, TAggregate, TRegistry> WithReadOneStreamFromStartCache(
          string streamId,
          IEventTypeProvider<TAggregate> eventTypeProvider,
          Action<MultipleStreamsCatchupCacheConfiguration<TAggregate>> getMultipleStreamsCatchupCacheConfiguration = null,
          ISnapshotStore<TAggregate> snapshotStore = null,
          ISnapshotStrategy snapshotStrategy = null)
        {
            return WithReadManyStreamsFromStartCache(new[] { streamId }, eventTypeProvider, getMultipleStreamsCatchupCacheConfiguration, snapshotStore, snapshotStrategy);
        }

        public EventStoreStatefulActorBuilder<TActor, TAggregate, TRegistry> WithReadManyStreamsFromStartCache(
          string[] streamIds,
          IEventTypeProvider<TAggregate> eventTypeProvider,
          Action<MultipleStreamsCatchupCacheConfiguration<TAggregate>> getMultipleStreamsCatchupCacheConfiguration = null,
          ISnapshotStore<TAggregate> snapshotStore = null,
          ISnapshotStrategy snapshotStrategy = null)
        {
            if (null != EventStoreCache) throw new InvalidOperationException($"A cache has already been set => {EventStoreCache.GetType()}");

            var multipleStreamsCatchupCacheConfiguration = new MultipleStreamsCatchupCacheConfiguration<TAggregate>(streamIds);

            getMultipleStreamsCatchupCacheConfiguration?.Invoke(multipleStreamsCatchupCacheConfiguration);

            EventStoreCache = new MultipleStreamsCatchupCache<TAggregate>(ConnectionMonitor, multipleStreamsCatchupCacheConfiguration, eventTypeProvider, LoggerFactory, snapshotStore, snapshotStrategy);

            return this;
        }

        public EventStoreStatefulActorBuilder<TActor, TAggregate, TRegistry> WithReadAllFromEndCache(
          IEventTypeProvider<TAggregate> eventTypeProvider,
          Action<AllStreamsCatchupCacheConfiguration<TAggregate>> getSubscribeFromEndCacheConfiguration = null)
        {

            if (null != EventStoreCache) throw new InvalidOperationException($"A cache has already been set => {EventStoreCache.GetType()}");

            var subscribeFromEndCacheConfiguration = new AllStreamsCatchupCacheConfiguration<TAggregate>(Position.End);

            getSubscribeFromEndCacheConfiguration?.Invoke(subscribeFromEndCacheConfiguration);

            EventStoreCache = new AllStreamsCatchupCache<TAggregate>(ConnectionMonitor, subscribeFromEndCacheConfiguration, eventTypeProvider, LoggerFactory);

            return this;

        }

        public EventStoreStatefulActorBuilder<TActor, TAggregate, TRegistry> WithBus<TBus>(Action<TActor, TBus> onStartup = null) where TBus : IBus
        {
            var busType = typeof(TBus);

            onStartup ??= new Action<TActor, TBus>((actor, bus) => { });

            if (_busToRegisterTo.ContainsKey(busType))
                throw new InvalidOperationException($"ActorBuilder already has a reference to a bus of type {busType}");

            var onRegistration = new Action<Container, IActor>((container, actor) =>
            {
                var bus = container.GetInstance<TBus>();

                onStartup((TActor)actor, bus);

            });

            _busToRegisterTo.Add(busType, onRegistration);

            return this;
        }
    }

}
