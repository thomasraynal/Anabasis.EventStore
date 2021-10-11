using System;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Shared;
using System.Collections.Generic;
using Anabasis.EventStore.Queue;
using Anabasis.EventStore.Snapshot;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.EventProvider;
using Microsoft.Extensions.DependencyInjection;

namespace Anabasis.EventStore
{

    public class StatefulActorBuilder<TActor, TKey, TAggregate>
        where TActor : IStatefulActor<TKey, TAggregate>
        where TAggregate : IAggregate<TKey>, new()
    {
        private readonly World _world;
  
        public StatefulActorBuilder(World world)
        {
            _world = world;
        }

        public World CreateActor()
        {
            _world.Add<TActor>(this);

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
                    eventTypeProvider,
                    snapshotStore,
                    snapshotStrategy);

                return catchupEventStoreCache;

            });

  
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
                    eventTypeProvider);

                return subscribeFromEndEventStoreCache;

            });

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
