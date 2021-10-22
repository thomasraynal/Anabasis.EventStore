using System;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Snapshot;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Mvc;
using System.Collections.Generic;
using Anabasis.EventStore.Queue;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace Anabasis.EventStore
{

    public class StatefulActorBuilder<TActor, TKey, TAggregate> : IStatefulActorBuilder
        where TActor : class, IStatefulActor<TKey, TAggregate>
        where TAggregate : IAggregate<TKey>, new()
    {
        private readonly World _world;
        private readonly List<Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreQueue>> _queuesToRegisterTo;
        private readonly IEventStoreCacheFactory _eventStoreCacheFactory;
        private Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreCache<TKey, TAggregate>> _cacheToRegisterTo;

        public StatefulActorBuilder(World world, IEventStoreCacheFactory eventStoreCacheFactory)
        {
            _queuesToRegisterTo = new List<Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreQueue>>();
            _eventStoreCacheFactory = eventStoreCacheFactory;
            _world = world;
        }

        public World CreateActor()
        {
            _world.StatefulActorBuilders.Add((typeof(TActor), this));
            _eventStoreCacheFactory.Add<TActor, TKey, TAggregate>(_cacheToRegisterTo);
            return _world;
        }

        public StatefulActorBuilder<TActor, TKey, TAggregate> WithReadAllFromStartCache(
          IEventTypeProvider<TKey, TAggregate> eventTypeProvider = null,
          Action<CatchupEventStoreCacheConfiguration<TKey, TAggregate>> catchupEventStoreCacheConfigurationBuilder = null,
          ISnapshotStore<TKey, TAggregate> snapshotStore = null,
          ISnapshotStrategy<TKey> snapshotStrategy = null)
        {

            var getCatchupEventStoreQueue = new Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreCache<TKey, TAggregate>>((connectionMonitor,loggerFactory) =>
            {
                var catchupEventStoreCacheConfiguration = new CatchupEventStoreCacheConfiguration<TKey, TAggregate>();

                catchupEventStoreCacheConfigurationBuilder?.Invoke(catchupEventStoreCacheConfiguration);

                var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TKey, TAggregate, TActor>();

                var catchupEventStoreCache = new CatchupEventStoreCache<TKey, TAggregate>(connectionMonitor,
                    catchupEventStoreCacheConfiguration,
                    eventProvider,
                    loggerFactory,
                    snapshotStore,
                    snapshotStrategy);

                return catchupEventStoreCache;

            });

            if (null != _cacheToRegisterTo) throw new InvalidOperationException("A cache as already been registered - only one cache allowed");

            _cacheToRegisterTo = getCatchupEventStoreQueue;

            return this;

        }

        public StatefulActorBuilder<TActor, TKey, TAggregate> WithReadOneStreamFromStartCache(
          string streamId,
          IEventTypeProvider<TKey, TAggregate> eventTypeProvider = null,
          Action<MultipleStreamsCatchupCacheConfiguration<TKey, TAggregate>> getMultipleStreamsCatchupCacheConfiguration = null,
          ISnapshotStore<TKey, TAggregate> snapshotStore = null,
          ISnapshotStrategy<TKey> snapshotStrategy = null)
        {
            return WithReadManyStreamFromStartCache(new[] { streamId }, eventTypeProvider, getMultipleStreamsCatchupCacheConfiguration, snapshotStore, snapshotStrategy);
        }

        public StatefulActorBuilder<TActor, TKey, TAggregate> WithReadManyStreamFromStartCache(
          string[] streamIds,
          IEventTypeProvider<TKey, TAggregate> eventTypeProvider = null,
          Action<MultipleStreamsCatchupCacheConfiguration<TKey, TAggregate>> getMultipleStreamsCatchupCacheConfiguration = null,
          ISnapshotStore<TKey, TAggregate> snapshotStore = null,
          ISnapshotStrategy<TKey> snapshotStrategy = null)
        {

            var getSubscribeFromEndMultipleStreamsEventStoreCache = new Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreCache<TKey, TAggregate>>((connectionMonitor, loggerFactory) =>
            {
                var multipleStreamsCatchupCacheConfiguration = new MultipleStreamsCatchupCacheConfiguration<TKey, TAggregate>(streamIds);

                getMultipleStreamsCatchupCacheConfiguration?.Invoke(multipleStreamsCatchupCacheConfiguration);

                var multipleStreamsCatchupCache = new MultipleStreamsCatchupCache<TKey, TAggregate>(connectionMonitor, multipleStreamsCatchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy);

                return multipleStreamsCatchupCache;
            });

            if (null != _cacheToRegisterTo) throw new InvalidOperationException("A cache as already been registered - only one cache allowed");

            _cacheToRegisterTo = getSubscribeFromEndMultipleStreamsEventStoreCache;

            return this;
        }

        public StatefulActorBuilder<TActor, TKey, TAggregate> WithReadAllFromEndCache(
          IEventTypeProvider<TKey, TAggregate> eventTypeProvider = null,
          Action<SubscribeFromEndCacheConfiguration<TKey, TAggregate>> volatileCacheConfigurationBuilder = null)
        {
            var getSubscribeFromEndEventStoreCache = new Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreCache<TKey, TAggregate>>((connectionMonitor, loggerFactory) =>
            {
                var subscribeFromEndCacheConfiguration = new SubscribeFromEndCacheConfiguration<TKey, TAggregate>();

                volatileCacheConfigurationBuilder?.Invoke(subscribeFromEndCacheConfiguration);

                var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TKey, TAggregate, TActor>();

                var subscribeFromEndEventStoreCache = new SubscribeFromEndEventStoreCache<TKey, TAggregate>(connectionMonitor,
                    subscribeFromEndCacheConfiguration,
                    eventProvider, loggerFactory);

                return subscribeFromEndEventStoreCache;

            });

            if (null != _cacheToRegisterTo) throw new InvalidOperationException("A cache as already been registered - only one cache allowed");

            _cacheToRegisterTo = getSubscribeFromEndEventStoreCache;

            return this;
        }

        public StatefulActorBuilder<TActor, TKey, TAggregate> WithSubscribeFromEndToAllQueue(
            Action<SubscribeFromEndEventStoreQueueConfiguration> getSubscribeFromEndEventStoreQueueConfiguration = null,
            IEventTypeProvider eventTypeProvider = null)
        {

            var getSubscribeFromEndEventStoreQueue = new Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreQueue>((connectionMonitor, loggerFactory) =>
            {
                var subscribeFromEndEventStoreQueueConfiguration = new SubscribeFromEndEventStoreQueueConfiguration();

                getSubscribeFromEndEventStoreQueueConfiguration?.Invoke(subscribeFromEndEventStoreQueueConfiguration);

                var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

                var subscribeFromEndEventStoreQueue = new SubscribeFromEndEventStoreQueue(
                  connectionMonitor,
                  subscribeFromEndEventStoreQueueConfiguration,
                  eventProvider, loggerFactory);

                return subscribeFromEndEventStoreQueue;

            });

            _queuesToRegisterTo.Add(getSubscribeFromEndEventStoreQueue);

            return this;
        }

        public StatefulActorBuilder<TActor, TKey, TAggregate> WithPersistentSubscriptionQueue(
            string streamId,
            string groupId,
            Action<PersistentSubscriptionEventStoreQueueConfiguration> getPersistentSubscriptionEventStoreQueueConfiguration = null)
        {
            var getPersistentSubscriptionEventStoreQueue = new Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreQueue>((connectionMonitor, loggerFactory) =>
            {
                var persistentEventStoreQueueConfiguration = new PersistentSubscriptionEventStoreQueueConfiguration(streamId, groupId);

                getPersistentSubscriptionEventStoreQueueConfiguration?.Invoke(persistentEventStoreQueueConfiguration);

                var eventProvider = new ConsumerBasedEventProvider<TActor>();

                var persistentSubscriptionEventStoreQueue = new PersistentSubscriptionEventStoreQueue(
                  connectionMonitor,
                  persistentEventStoreQueueConfiguration,
                  eventProvider,
                  loggerFactory);

                return persistentSubscriptionEventStoreQueue;

            });

            _queuesToRegisterTo.Add(getPersistentSubscriptionEventStoreQueue);

            return this;
        }

        public StatefulActorBuilder<TActor, TKey, TAggregate> WithSubscribeFromStartToOneStreamQueue(
            string streamId,
            Action<SubscribeToOneStreamFromStartOrLaterEventStoreQueueConfiguration> getSubscribeFromEndToOneStreamEventStoreQueueConfiguration = null,
            IEventTypeProvider eventTypeProvider = null)
        {
            var getSubscribeFromEndToOneStreamQueue = new Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreQueue>((connectionMonitor,loggerFactory) =>
            {

                var subscribeFromEndToOneStreamEventStoreQueueConfiguration = new SubscribeToOneStreamFromStartOrLaterEventStoreQueueConfiguration(streamId);

                getSubscribeFromEndToOneStreamEventStoreQueueConfiguration?.Invoke(subscribeFromEndToOneStreamEventStoreQueueConfiguration);

                var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

                var subscribeFromEndToOneStreamEventStoreQueue = new SubscribeFromStartOrLaterToOneStreamEventStoreQueue(
                  connectionMonitor,
                  subscribeFromEndToOneStreamEventStoreQueueConfiguration,
                  eventProvider, loggerFactory);

                return subscribeFromEndToOneStreamEventStoreQueue;

            });

            _queuesToRegisterTo.Add(getSubscribeFromEndToOneStreamQueue);

            return this;

        }

        public StatefulActorBuilder<TActor, TKey, TAggregate> WithSubscribeFromEndToOneStreamQueue(
            string streamId,
            Action<SubscribeToOneStreamFromStartOrLaterEventStoreQueueConfiguration> getSubscribeFromEndToOneStreamEventStoreQueueConfiguration = null,
            IEventTypeProvider eventTypeProvider = null)
        {
            var getSubscribeFromEndToOneStreamQueue = new Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreQueue>((connectionMonitor, loggerFactory) =>
            {

                var subscribeFromEndToOneStreamEventStoreQueueConfiguration = new SubscribeToOneStreamFromStartOrLaterEventStoreQueueConfiguration(streamId);

                getSubscribeFromEndToOneStreamEventStoreQueueConfiguration?.Invoke(subscribeFromEndToOneStreamEventStoreQueueConfiguration);

                var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

                var subscribeFromEndToOneStreamEventStoreQueue = new SubscribeFromEndToOneStreamEventStoreQueue(
                  connectionMonitor,
                  subscribeFromEndToOneStreamEventStoreQueueConfiguration,
                  eventProvider,
                  loggerFactory);

                return subscribeFromEndToOneStreamEventStoreQueue;

            });

            _queuesToRegisterTo.Add(getSubscribeFromEndToOneStreamQueue);

            return this;

        }

        public Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreQueue>[] GetQueueFactories()
        {
            return _queuesToRegisterTo.ToArray();
        }
    }


}
