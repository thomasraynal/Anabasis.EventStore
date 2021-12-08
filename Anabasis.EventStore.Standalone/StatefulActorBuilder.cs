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
    public class StatefulActorBuilder<TActor, TKey, TAggregate, TRegistry>
      where TActor : IStatefulActor<TKey, TAggregate>
      where TAggregate : IAggregate<TKey>, new()
      where TRegistry : ServiceRegistry, new()
    {
        private IEventStoreCache<TKey, TAggregate> EventStoreCache { get; set; }
        private IEventStoreAggregateRepository<TKey> EventStoreRepository { get; set; }
        private ILoggerFactory LoggerFactory { get; set; }
        private ConnectionStatusMonitor ConnectionMonitor { get; set; }

        private readonly List<IEventStream> _streamsToRegisterTo;

        private StatefulActorBuilder()
        {
            _streamsToRegisterTo = new List<IEventStream>();
        }

        public TActor Build()
        {
            if (null == EventStoreCache) throw new InvalidOperationException($"You must specify a cache for an StatefulActor");

            var container = new Container(configuration =>
            {
                configuration.For<ILoggerFactory>().Use(LoggerFactory);
                configuration.For<IEventStoreCache<TKey, TAggregate>>().Use(EventStoreCache);
                configuration.For<IEventStoreAggregateRepository<TKey>>().Use(EventStoreRepository);
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

        public static StatefulActorBuilder<TActor, TKey, TAggregate, TRegistry> Create(
        string eventStoreUrl,
        ConnectionSettings connectionSettings,
        ILoggerFactory loggerFactory = null,
        Action<IEventStoreRepositoryConfiguration> getEventStoreRepositoryConfigurationBuilder = null)
        {
            var connection = EventStoreConnection.Create(connectionSettings, new Uri(eventStoreUrl));

            return CreateInternal(connection, loggerFactory, getEventStoreRepositoryConfigurationBuilder);

        }


        public static StatefulActorBuilder<TActor, TKey, TAggregate, TRegistry> Create(ClusterVNode clusterVNode,
          ConnectionSettings connectionSettings,
          ILoggerFactory loggerFactory = null,
          Action<IEventStoreRepositoryConfiguration> eventStoreRepositoryConfigurationBuilder = null)
        {

            var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);

            return CreateInternal(connection, loggerFactory, eventStoreRepositoryConfigurationBuilder);

        }

        private static StatefulActorBuilder<TActor, TKey, TAggregate, TRegistry> CreateInternal(
          IEventStoreConnection eventStoreConnection,
          ILoggerFactory loggerFactory = null,
          Action<IEventStoreRepositoryConfiguration> eventStoreRepositoryConfigurationBuilder = null)
        {

            var builder = new StatefulActorBuilder<TActor, TKey, TAggregate, TRegistry>
            {
                LoggerFactory = loggerFactory?? new DummyLoggerFactory(),
                ConnectionMonitor = new ConnectionStatusMonitor(eventStoreConnection, loggerFactory)
            };

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();

            eventStoreRepositoryConfigurationBuilder?.Invoke(eventStoreRepositoryConfiguration);

            builder.EventStoreRepository = new EventStoreAggregateRepository<TKey>(
              eventStoreRepositoryConfiguration,
              eventStoreConnection,
              builder.ConnectionMonitor,
              loggerFactory);

            return builder;

        }

        public StatefulActorBuilder<TActor, TKey, TAggregate, TRegistry> WithReadAllFromStartCache(
          IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
          Action<AllStreamsCatchupCacheConfiguration<TKey, TAggregate>> getCatchupEventStoreCacheConfigurationBuilder = null,
          ISnapshotStore<TKey, TAggregate> snapshotStore = null,
          ISnapshotStrategy<TKey> snapshotStrategy = null)
        {
            if (null != EventStoreCache) throw new InvalidOperationException($"A cache has already been set => {EventStoreCache.GetType()}");

            var catchupEventStoreCacheConfiguration = new AllStreamsCatchupCacheConfiguration<TKey, TAggregate>(Position.Start);

            getCatchupEventStoreCacheConfigurationBuilder?.Invoke(catchupEventStoreCacheConfiguration);

            EventStoreCache = new AllStreamsCatchupCache<TKey, TAggregate>(ConnectionMonitor, catchupEventStoreCacheConfiguration, eventTypeProvider, LoggerFactory, snapshotStore, snapshotStrategy);

            return this;
        }

        public StatefulActorBuilder<TActor, TKey, TAggregate, TRegistry> WithReadOneStreamFromStartCache(
          string streamId,
          IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
          Action<MultipleStreamsCatchupCacheConfiguration<TKey, TAggregate>> getMultipleStreamsCatchupCacheConfiguration = null,
          ISnapshotStore<TKey, TAggregate> snapshotStore = null,
          ISnapshotStrategy<TKey> snapshotStrategy = null)
        {
            return WithReadManyStreamFromStartCache(new[] { streamId }, eventTypeProvider, getMultipleStreamsCatchupCacheConfiguration, snapshotStore, snapshotStrategy);
        }

        public StatefulActorBuilder<TActor, TKey, TAggregate, TRegistry> WithReadManyStreamFromStartCache(
          string[] streamIds,
          IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
          Action<MultipleStreamsCatchupCacheConfiguration<TKey, TAggregate>> getMultipleStreamsCatchupCacheConfiguration = null,
          ISnapshotStore<TKey, TAggregate> snapshotStore = null,
          ISnapshotStrategy<TKey> snapshotStrategy = null)
        {
            if (null != EventStoreCache) throw new InvalidOperationException($"A cache has already been set => {EventStoreCache.GetType()}");

            var multipleStreamsCatchupCacheConfiguration = new MultipleStreamsCatchupCacheConfiguration<TKey, TAggregate>(streamIds);

            getMultipleStreamsCatchupCacheConfiguration?.Invoke(multipleStreamsCatchupCacheConfiguration);

            EventStoreCache = new MultipleStreamsCatchupCache<TKey, TAggregate>(ConnectionMonitor, multipleStreamsCatchupCacheConfiguration, eventTypeProvider, LoggerFactory, snapshotStore, snapshotStrategy);

            return this;
        }

        public StatefulActorBuilder<TActor, TKey, TAggregate, TRegistry> WithReadAllFromEndCache(
          IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
          Action<AllStreamsCatchupCacheConfiguration<TKey, TAggregate>> getSubscribeFromEndCacheConfiguration = null)
        {

            if (null != EventStoreCache) throw new InvalidOperationException($"A cache has already been set => {EventStoreCache.GetType()}");

            var subscribeFromEndCacheConfiguration = new AllStreamsCatchupCacheConfiguration<TKey, TAggregate>(Position.End);

            getSubscribeFromEndCacheConfiguration?.Invoke(subscribeFromEndCacheConfiguration);

            EventStoreCache = new AllStreamsCatchupCache<TKey, TAggregate>(ConnectionMonitor, subscribeFromEndCacheConfiguration, eventTypeProvider, LoggerFactory);

            return this;

        }

        public StatefulActorBuilder<TActor, TKey, TAggregate, TRegistry> WithSubscribeFromEndToAllStream(
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

        public StatefulActorBuilder<TActor, TKey, TAggregate, TRegistry> WithPersistentSubscriptionStream(string streamId, string groupId)
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

    }

}
