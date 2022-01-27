using System;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Stream;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Anabasis.Common;
using System.Threading.Tasks;

namespace Anabasis.EventStore
{
    public class StatelessActorBuilder<TActor> : IStatelessActorBuilder
        where TActor : IEventStoreStatelessActor
    {
        private readonly World _world;
        private readonly List<Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreStream>> _streamsToRegisterTo;

        public StatelessActorBuilder(World world)
        {
            _world = world;
            _streamsToRegisterTo = new List<Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreStream>>();
        }

        public StatelessActorBuilder<TActor> WithSubscribeFromEndToAllStreams(
            Action<SubscribeFromEndEventStoreStreamConfiguration> getSubscribeFromEndEventStoreStreamConfiguration = null,  
            IEventTypeProvider eventTypeProvider = null)
        {
            var getSubscribeFromEndEventStoreStream = new Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreStream>((connectionMonitor, loggerFactory) =>
            {
                var subscribeFromEndEventStoreStreamConfiguration = new SubscribeFromEndEventStoreStreamConfiguration();

                getSubscribeFromEndEventStoreStreamConfiguration?.Invoke(subscribeFromEndEventStoreStreamConfiguration);

                var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

                var subscribeFromEndEventStoreStream = new SubscribeFromEndEventStoreStream(
                  connectionMonitor,
                  subscribeFromEndEventStoreStreamConfiguration,
                  eventProvider,
                  loggerFactory);

                return subscribeFromEndEventStoreStream;

            });

            _streamsToRegisterTo.Add(getSubscribeFromEndEventStoreStream);

            return this;
        }

        public StatelessActorBuilder<TActor> WithSubscribeFromStartToOneStreamStream(
            string streamId,
            Action<SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration> getSubscribeFromEndToOneStreamEventStoreStreamConfiguration = null,
            IEventTypeProvider eventTypeProvider = null)
        {
            var getSubscribeFromEndToOneStreamStream = new Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreStream>((connectionMonitor, loggerFactory) =>
            {

                var subscribeFromEndToOneStreamEventStoreStreamConfiguration = new SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration(streamId);

                getSubscribeFromEndToOneStreamEventStoreStreamConfiguration?.Invoke(subscribeFromEndToOneStreamEventStoreStreamConfiguration);

                var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

                var subscribeFromEndToOneStreamEventStoreStream = new SubscribeFromStartOrLaterToOneStreamEventStoreStream(
                  connectionMonitor,
                  subscribeFromEndToOneStreamEventStoreStreamConfiguration,
                  eventProvider,
                  loggerFactory);

                return subscribeFromEndToOneStreamEventStoreStream;

            });

            _streamsToRegisterTo.Add(getSubscribeFromEndToOneStreamStream);

            return this;

        }

        public StatelessActorBuilder<TActor> WithSubscribeFromEndToOneStreamStream(
            string streamId,
            Action<SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration> getSubscribeFromEndToOneStreamEventStoreStreamConfiguration = null,
            IEventTypeProvider eventTypeProvider = null)
        {
            var getSubscribeFromEndToOneStreamStream = new Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreStream>((connectionMonitor, loggerFactory) =>
            {

                var subscribeFromEndToOneStreamEventStoreStreamConfiguration = new SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration(streamId);

                getSubscribeFromEndToOneStreamEventStoreStreamConfiguration?.Invoke(subscribeFromEndToOneStreamEventStoreStreamConfiguration);

                var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

                var subscribeFromEndToOneStreamEventStoreStream = new SubscribeFromEndToOneStreamEventStoreStream(
                  connectionMonitor,
                  subscribeFromEndToOneStreamEventStoreStreamConfiguration,
                  eventProvider, loggerFactory);

                return subscribeFromEndToOneStreamEventStoreStream;

            });

            _streamsToRegisterTo.Add(getSubscribeFromEndToOneStreamStream);

            return this;

        }

        public StatelessActorBuilder<TActor> WithPersistentSubscriptionStream(
            string streamId, 
            string groupId,
            Action<PersistentSubscriptionEventStoreStreamConfiguration> getPersistentSubscriptionEventStoreStreamConfiguration = null)
        {
            var getPersistentSubscriptionEventStoreStream = new Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreStream>((connectionMonitor, loggerFactory) =>
            {
                var persistentEventStoreStreamConfiguration = new PersistentSubscriptionEventStoreStreamConfiguration(streamId, groupId);

                getPersistentSubscriptionEventStoreStreamConfiguration?.Invoke(persistentEventStoreStreamConfiguration);

                var eventProvider = new ConsumerBasedEventProvider<TActor>();

                var persistentSubscriptionEventStoreStream = new PersistentSubscriptionEventStoreStream(
                  connectionMonitor,
                  persistentEventStoreStreamConfiguration,
                  eventProvider,
                  loggerFactory);

                return persistentSubscriptionEventStoreStream;

            });

            _streamsToRegisterTo.Add(getPersistentSubscriptionEventStoreStream);

            return this;
        }

        public World CreateActor()
        {
            _world.StatelessActorBuilders.Add((typeof(TActor), this));

            return _world;
        }

        public Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreStream>[] GetStreamFactories()
        {
            return _streamsToRegisterTo.ToArray();
        }

    }
}
