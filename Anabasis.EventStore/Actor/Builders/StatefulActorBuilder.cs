using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Queue;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Snapshot;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.Core;
using Lamar;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Anabasis.EventStore.Actor
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

        private readonly List<IEventStoreQueue> _queuesToRegisterTo;

        private StatefulActorBuilder()
        {
            _queuesToRegisterTo = new List<IEventStoreQueue>();
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

            foreach (var queue in _queuesToRegisterTo)
            {
                actor.SubscribeTo(queue, closeSubscriptionOnDispose: true);
            }

            return actor;

        }

        public static StatefulActorBuilder<TActor, TKey, TAggregate, TRegistry> Create(
        string eventStoreUrl,
        ConnectionSettings connectionSettings,
        ILoggerFactory loggerFactory = null,
        Action<IEventStoreRepositoryConfiguration> eventStoreRepositoryConfigurationBuilder = null)
        {
            var connection = EventStoreConnection.Create(connectionSettings, new Uri(eventStoreUrl));

            return CreateInternal(connection, loggerFactory, eventStoreRepositoryConfigurationBuilder);

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
                LoggerFactory = loggerFactory,
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
          Action<CatchupEventStoreCacheConfiguration<TKey, TAggregate>> catchupEventStoreCacheConfigurationBuilder = null,
          ISnapshotStore<TKey, TAggregate> snapshotStore = null,
          ISnapshotStrategy<TKey> snapshotStrategy = null)
        {
            if (null != EventStoreCache) throw new InvalidOperationException($"A cache has already been set => {EventStoreCache.GetType()}");

            var catchupEventStoreCacheConfiguration = new CatchupEventStoreCacheConfiguration<TKey, TAggregate>();

            catchupEventStoreCacheConfigurationBuilder?.Invoke(catchupEventStoreCacheConfiguration);

            EventStoreCache = new CatchupEventStoreCache<TKey, TAggregate>(ConnectionMonitor, catchupEventStoreCacheConfiguration, eventTypeProvider, LoggerFactory, snapshotStore, snapshotStrategy);

            return this;
        }

        public StatefulActorBuilder<TActor, TKey, TAggregate, TRegistry> WithReadOneStreamFromStartCache(
          string streamId,
          IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
          Action<SingleStreamCatchupEventStoreCacheConfiguration<TKey, TAggregate>> singleStreamCatchupEventStoreCacheConfigurationBuilder = null,
          ISnapshotStore<TKey, TAggregate> snapshotStore = null,
          ISnapshotStrategy<TKey> snapshotStrategy = null)
        {
            if (null != EventStoreCache) throw new InvalidOperationException($"A cache has already been set => {EventStoreCache.GetType()}");

            var singleStreamCatchupEventStoreCacheConfiguration = new SingleStreamCatchupEventStoreCacheConfiguration<TKey, TAggregate>(streamId);

            singleStreamCatchupEventStoreCacheConfigurationBuilder?.Invoke(singleStreamCatchupEventStoreCacheConfiguration);

            EventStoreCache = new SingleStreamCatchupEventStoreCache<TKey, TAggregate>(ConnectionMonitor, singleStreamCatchupEventStoreCacheConfiguration, eventTypeProvider, LoggerFactory, snapshotStore, snapshotStrategy);

            return this;
        }

        public StatefulActorBuilder<TActor, TKey, TAggregate, TRegistry> WithReadAllFromEndCache(
          IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
          Action<SubscribeFromEndCacheConfiguration<TKey, TAggregate>> volatileCacheConfigurationBuilder = null)
        {

            if (null != EventStoreCache) throw new InvalidOperationException($"A cache has already been set => {EventStoreCache.GetType()}");

            var volatileCacheConfiguration = new SubscribeFromEndCacheConfiguration<TKey, TAggregate>();

            volatileCacheConfigurationBuilder?.Invoke(volatileCacheConfiguration);

            EventStoreCache = new SubscribeFromEndEventStoreCache<TKey, TAggregate>(ConnectionMonitor, volatileCacheConfiguration, eventTypeProvider, LoggerFactory);

            return this;

        }

        public StatefulActorBuilder<TActor, TKey, TAggregate, TRegistry> WithSubscribeFromEndToAllQueue()
        {
            var volatileEventStoreQueueConfiguration = new SubscribeFromEndEventStoreQueueConfiguration();

            var eventProvider = new ConsumerBasedEventProvider<TActor>();

            var volatileEventStoreQueue = new SubscribeFromEndEventStoreQueue(
              ConnectionMonitor,
              volatileEventStoreQueueConfiguration,
              eventProvider, LoggerFactory);

            _queuesToRegisterTo.Add(volatileEventStoreQueue);

            return this;
        }

        public StatefulActorBuilder<TActor, TKey, TAggregate, TRegistry> WithPersistentSubscriptionQueue(string streamId, string groupId)
        {
            var persistentEventStoreQueueConfiguration = new PersistentSubscriptionEventStoreQueueConfiguration(streamId, groupId);

            var eventProvider = new ConsumerBasedEventProvider<TActor>();

            var persistentSubscriptionEventStoreQueue = new PersistentSubscriptionEventStoreQueue(
              ConnectionMonitor,
              persistentEventStoreQueueConfiguration,
              eventProvider, LoggerFactory);

            _queuesToRegisterTo.Add(persistentSubscriptionEventStoreQueue);

            return this;
        }

    }

}