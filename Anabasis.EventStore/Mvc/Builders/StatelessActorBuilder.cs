using System;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Queue;
using System.Collections.Generic;

namespace Anabasis.EventStore
{
    public class StatelessActorBuilder<TActor> : IStatelessActorBuilder
        where TActor : IStatelessActor
    {
        private readonly World _world;
        private readonly List<Func<IConnectionStatusMonitor, IEventStoreQueue>> _queuesToRegisterTo;

        public StatelessActorBuilder(World world)
        {
            _world = world;
            _queuesToRegisterTo = new List<Func<IConnectionStatusMonitor, IEventStoreQueue>>();
        }


        public StatelessActorBuilder<TActor> WithSubscribeFromEndToAllQueue(
            Action<SubscribeFromEndEventStoreQueueConfiguration> getSubscribeFromEndEventStoreQueueConfiguration = null,  
            IEventTypeProvider eventTypeProvider = null)
        {
            var getSubscribeFromEndEventStoreQueue = new Func<IConnectionStatusMonitor, IEventStoreQueue>((connectionMonitor) =>
            {
                var subscribeFromEndEventStoreQueueConfiguration = new SubscribeFromEndEventStoreQueueConfiguration();

                getSubscribeFromEndEventStoreQueueConfiguration?.Invoke(subscribeFromEndEventStoreQueueConfiguration);

                var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

                var subscribeFromEndEventStoreQueue = new SubscribeFromEndEventStoreQueue(
                  connectionMonitor,
                  subscribeFromEndEventStoreQueueConfiguration,
                  eventProvider);

                return subscribeFromEndEventStoreQueue;

            });

            _queuesToRegisterTo.Add(getSubscribeFromEndEventStoreQueue);

            return this;
        }

        public StatelessActorBuilder<TActor> WithSubscribeFromEndToOneStreamQueue(
            string streamId,
            Action<SubscribeFromEndToOneStreamEventStoreQueueConfiguration> getSubscribeFromEndToOneStreamEventStoreQueueConfiguration = null,
            IEventTypeProvider eventTypeProvider = null)
        {
            var getSubscribeFromEndToOneStreamQueue = new Func<IConnectionStatusMonitor, IEventStoreQueue>((connectionMonitor) =>
            {

                var subscribeFromEndToOneStreamEventStoreQueueConfiguration = new SubscribeFromEndToOneStreamEventStoreQueueConfiguration(streamId);

                getSubscribeFromEndToOneStreamEventStoreQueueConfiguration?.Invoke(subscribeFromEndToOneStreamEventStoreQueueConfiguration);

                var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

                var subscribeFromEndToOneStreamEventStoreQueue = new SubscribeFromEndToOneStreamEventStoreQueue(
                  connectionMonitor,
                  subscribeFromEndToOneStreamEventStoreQueueConfiguration,
                  eventProvider);

                return subscribeFromEndToOneStreamEventStoreQueue;

            });

            _queuesToRegisterTo.Add(getSubscribeFromEndToOneStreamQueue);

            return this;

        }

        public StatelessActorBuilder<TActor> WithPersistentSubscriptionQueue(
            string streamId, 
            string groupId,
            Action<PersistentSubscriptionEventStoreQueueConfiguration> getPersistentSubscriptionEventStoreQueueConfiguration = null)
        {
            var getPersistentSubscriptionEventStoreQueue = new Func<IConnectionStatusMonitor, IEventStoreQueue>((connectionMonitor) =>
            {
                var persistentEventStoreQueueConfiguration = new PersistentSubscriptionEventStoreQueueConfiguration(streamId, groupId);

                getPersistentSubscriptionEventStoreQueueConfiguration?.Invoke(persistentEventStoreQueueConfiguration);

                var eventProvider = new ConsumerBasedEventProvider<TActor>();

                var persistentSubscriptionEventStoreQueue = new PersistentSubscriptionEventStoreQueue(
                  connectionMonitor,
                  persistentEventStoreQueueConfiguration,
                  eventProvider);

                return persistentSubscriptionEventStoreQueue;

            });

            _queuesToRegisterTo.Add(getPersistentSubscriptionEventStoreQueue);

            return this;
        }

        public World CreateActor()
        {
            _world.StatelessActorBuilders.Add((typeof(TActor), this));

            return _world;
        }

        public Func<IConnectionStatusMonitor, IEventStoreQueue>[] GetQueueFactories()
        {
            return _queuesToRegisterTo.ToArray();
        }
    }
}
