using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Stream;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Snapshot;
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
    public class EventStoreStatefulActorBuilder<TActor, TAggregate, TRegistry>
      where TActor : IEventStoreStatefulActor<TAggregate>
      where TAggregate : class, IAggregate, new()
      where TRegistry : ServiceRegistry, new()
    {
        private IEventStoreCache<TAggregate> EventStoreCache { get; set; }
        private IEventStoreAggregateRepository EventStoreRepository { get; set; }
        private ILoggerFactory LoggerFactory { get; set; }
        private IConnectionStatusMonitor<IEventStoreConnection> ConnectionMonitor { get; set; }
        private IActorConfiguration ActorConfiguration { get; set; }

        private readonly List<IEventStoreStream> _streamsToRegisterTo;
        private readonly Dictionary<Type, Action<Container, IActor>> _busToRegisterTo;

        private EventStoreStatefulActorBuilder()
        {
            _streamsToRegisterTo = new List<IEventStoreStream>();
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
                configuration.For<IEventStoreCache<TAggregate>>().Use(EventStoreCache);
                configuration.For<IEventStoreAggregateRepository>().Use(EventStoreRepository);
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

                if (null == bus)
                    throw new InvalidOperationException($"No bus of type {busRegistration.Key} has been registered");

                bus.Initialize().Wait();
                actor.ConnectTo(bus).Wait();

                var onBusRegistration = busRegistration.Value;

                onBusRegistration(container, actor);

            }

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

        public static EventStoreStatefulActorBuilder<TActor, TAggregate, TRegistry> Create(ClusterVNode clusterVNode,
          ConnectionSettings connectionSettings,
          IActorConfiguration actorConfiguration,
          ILoggerFactory loggerFactory = null,
          Action<IEventStoreRepositoryConfiguration> eventStoreRepositoryConfigurationBuilder = null)
        {

            var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);

            return CreateInternal(actorConfiguration, connection, loggerFactory, eventStoreRepositoryConfigurationBuilder);

        }

        private static EventStoreStatefulActorBuilder<TActor, TAggregate, TRegistry> CreateInternal(
          IActorConfiguration actorConfiguration,
          IEventStoreConnection eventStoreConnection,
          ILoggerFactory loggerFactory = null,
          Action<IEventStoreRepositoryConfiguration> eventStoreRepositoryConfigurationBuilder = null)
        {

            var builder = new EventStoreStatefulActorBuilder<TActor, TAggregate, TRegistry>
            {
                LoggerFactory = loggerFactory ?? new DummyLoggerFactory(),
                ConnectionMonitor = new EventStoreConnectionStatusMonitor(eventStoreConnection, loggerFactory),
                ActorConfiguration = actorConfiguration
            };

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();

            eventStoreRepositoryConfigurationBuilder?.Invoke(eventStoreRepositoryConfiguration);

            builder.EventStoreRepository = new EventStoreAggregateRepository(
              eventStoreRepositoryConfiguration,
              eventStoreConnection,
              builder.ConnectionMonitor,
              loggerFactory);

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

        public EventStoreStatefulActorBuilder<TActor, TAggregate, TRegistry> WithSubscribeFromEndToAllStream(
            Action<SubscribeFromEndEventStoreStreamConfiguration> getSubscribeFromEndEventStoreStreamConfiguration = null)
        {
            var subscribeFromEndEventStoreStreamConfiguration = new SubscribeFromEndEventStoreStreamConfiguration();

            getSubscribeFromEndEventStoreStreamConfiguration?.Invoke(subscribeFromEndEventStoreStreamConfiguration);

            var eventProvider = new ConsumerBasedEventProvider<TActor>();

            var volatileEventStoreStream = new SubscribeFromEndEventStoreStream(
              ConnectionMonitor,
              subscribeFromEndEventStoreStreamConfiguration,
              eventProvider, LoggerFactory);

            _streamsToRegisterTo.Add(volatileEventStoreStream);

            return this;
        }

        public EventStoreStatefulActorBuilder<TActor, TAggregate, TRegistry> WithPersistentSubscriptionStream(string streamId, string groupId)
        {
            var persistentEventStoreStreamConfiguration = new PersistentSubscriptionEventStoreStreamConfiguration(streamId, groupId);

            var eventProvider = new ConsumerBasedEventProvider<TActor>();

            var persistentSubscriptionEventStoreStream = new PersistentSubscriptionEventStoreStream(
              ConnectionMonitor,
              persistentEventStoreStreamConfiguration,
              eventProvider, LoggerFactory);

            _streamsToRegisterTo.Add(persistentSubscriptionEventStoreStream);

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
