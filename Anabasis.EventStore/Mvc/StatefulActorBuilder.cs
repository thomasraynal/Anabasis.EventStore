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

namespace Anabasis.EventStore
{

    public class StatefulActorBuilder<TActor, TKey, TAggregate> : IStatefulActorBuilder
        where TActor : class, IStatefulActor<TKey, TAggregate>
        where TAggregate : IAggregate<TKey>, new()
    {
        private readonly World _world;
        private readonly List<Func<IConnectionStatusMonitor, IEventStoreQueue>> _queuesToRegisterTo;
        private readonly IEventStoreCacheFactory _eventStoreCacheFactory;
        private Func<IConnectionStatusMonitor, IEventStoreCache<TKey, TAggregate>> _cacheToRegisterTo;

        public StatefulActorBuilder(World world, IEventStoreCacheFactory eventStoreCacheFactory)
        {
            _queuesToRegisterTo = new List<Func<IConnectionStatusMonitor, IEventStoreQueue>>();
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

            var getCatchupEventStoreQueue = new Func<IConnectionStatusMonitor, IEventStoreCache<TKey, TAggregate>>((connectionMonitor) =>
            {
                var catchupEventStoreCacheConfiguration = new CatchupEventStoreCacheConfiguration<TKey, TAggregate>();

                catchupEventStoreCacheConfigurationBuilder?.Invoke(catchupEventStoreCacheConfiguration);

                var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TKey, TAggregate, TActor>();

                var catchupEventStoreCache = new CatchupEventStoreCache<TKey, TAggregate>(connectionMonitor,
                    catchupEventStoreCacheConfiguration,
                    eventProvider,
                    snapshotStore,
                    snapshotStrategy);

                return catchupEventStoreCache;

            });

            if (null != _cacheToRegisterTo) throw new InvalidOperationException("A cache as already been registered");

            _cacheToRegisterTo = getCatchupEventStoreQueue;

            return this;

        }

        public StatefulActorBuilder<TActor, TKey, TAggregate> WithReadOneStreamFromStartCache(
          string streamId,
          IEventTypeProvider<TKey, TAggregate> eventTypeProvider = null,
          Action<SingleStreamCatchupEventStoreCacheConfiguration<TKey, TAggregate>> singleStreamCatchupEventStoreCacheConfigurationBuilder = null,
          ISnapshotStore<TKey, TAggregate> snapshotStore = null,
          ISnapshotStrategy<TKey> snapshotStrategy = null)
        {
            return this;
        }

        public StatefulActorBuilder<TActor, TKey, TAggregate> WithReadAllFromEndCache(
          IEventTypeProvider<TKey, TAggregate> eventTypeProvider = null,
          Action<SubscribeFromEndCacheConfiguration<TKey, TAggregate>> volatileCacheConfigurationBuilder = null)
        {
            var getSubscribeFromEndEventStoreCache = new Func<IConnectionStatusMonitor, IEventStoreCache<TKey, TAggregate>>((connectionMonitor) =>
            {
                var subscribeFromEndCacheConfiguration = new SubscribeFromEndCacheConfiguration<TKey, TAggregate>();

                volatileCacheConfigurationBuilder?.Invoke(subscribeFromEndCacheConfiguration);

                var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TKey, TAggregate, TActor>();

                var subscribeFromEndEventStoreCache = new SubscribeFromEndEventStoreCache<TKey, TAggregate>(connectionMonitor,
                    subscribeFromEndCacheConfiguration,
                    eventProvider);

                return subscribeFromEndEventStoreCache;

            });

            if (null != _cacheToRegisterTo) throw new InvalidOperationException("A cache as already been registered");

            _cacheToRegisterTo = getSubscribeFromEndEventStoreCache;

            return this;
        }

        public StatefulActorBuilder<TActor, TKey, TAggregate> WithSubscribeToAllQueue()
        {
            return this;
        }

        public StatefulActorBuilder<TActor, TKey, TAggregate> WithPersistentSubscriptionQueue(string streamId, string groupId)
        {
            return this;
        }

    }


}
