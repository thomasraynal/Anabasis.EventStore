using System;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Stream;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Anabasis.Common;
using System.Linq;
using EventStore.ClientAPI;

namespace Anabasis.EventStore.AspNet.Builders
{
    public class EventStoreStatelessActorBuilder<TActor> : IEventStoreStatelessActorBuilder
        where TActor : IEventStoreStatelessActor
    {
        private readonly World _world;
        private readonly List<Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IEventStoreStream>> _streamsToRegisterTo;
        private readonly Dictionary<Type, Action<IServiceProvider, IActor>> _busToRegisterTo;

        public EventStoreStatelessActorBuilder(World world)
        {
            _world = world;
            _streamsToRegisterTo = new List<Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IEventStoreStream>>();
            _busToRegisterTo = new Dictionary<Type, Action<IServiceProvider, IActor>>();
        }

        public EventStoreStatelessActorBuilder<TActor> WithSubscribeFromEndToAllStreams(
            Action<SubscribeFromEndEventStoreStreamConfiguration> getSubscribeFromEndEventStoreStreamConfiguration = null,  
            IEventTypeProvider eventTypeProvider = null)
        {
            var getSubscribeFromEndEventStoreStream = new Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IEventStoreStream>((connectionMonitor, loggerFactory) =>
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

        public EventStoreStatelessActorBuilder<TActor> WithSubscribeFromStartToOneStream(
            string streamId,
            Action<SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration> getSubscribeFromEndToOneStreamEventStoreStreamConfiguration = null,
            IEventTypeProvider eventTypeProvider = null)
        {
            var getSubscribeFromEndToOneStreamStream = new Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IEventStoreStream>((connectionMonitor, loggerFactory) =>
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

        public EventStoreStatelessActorBuilder<TActor> WithSubscribeFromEndToOneStream(
            string streamId,
            Action<SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration> getSubscribeFromEndToOneStreamEventStoreStreamConfiguration = null,
            IEventTypeProvider eventTypeProvider = null)
        {
            var getSubscribeFromEndToOneStreamStream = new Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IEventStoreStream>((connectionMonitor, loggerFactory) =>
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

        public EventStoreStatelessActorBuilder<TActor> WithPersistentSubscriptionStream(
            string streamId, 
            string groupId,
            Action<PersistentSubscriptionEventStoreStreamConfiguration> getPersistentSubscriptionEventStoreStreamConfiguration = null)
        {
            var getPersistentSubscriptionEventStoreStream = new Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IEventStoreStream>((connectionMonitor, loggerFactory) =>
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

        public EventStoreStatelessActorBuilder<TActor> WithBus<TBus>(Action<TActor, TBus> onStartup = null) where TBus : IBus
        {
            var busType = typeof(TBus);

            onStartup ??= new Action<TActor, TBus>((actor, bus) => { });

            if (_busToRegisterTo.ContainsKey(busType))
                throw new InvalidOperationException($"ActorBuilder already has a reference to a bus of type {busType}");

            var onRegistration = new Action<IServiceProvider, IActor>((serviceProvider, actor) =>
            {
                var bus = (TBus)serviceProvider.GetService(busType);

                if(null == bus)
                    throw new InvalidOperationException($"No bus of type {busType} has been registered");

                bus.Initialize().Wait();
                actor.ConnectTo(bus).Wait();

                onStartup?.Invoke((TActor)actor, bus);

            });

            _busToRegisterTo.Add(busType, onRegistration);

            return this;
        }

        public World CreateActor()
        {
            _world.EventStoreStatelessActorBuilders.Add((typeof(TActor), this));
            return _world;
        }

        public Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IEventStoreStream>[] GetStreamFactories()
        {
            return _streamsToRegisterTo.ToArray();
        }

        public (Type actor, Action<IServiceProvider, IActor> factory)[] GetBusFactories()
        {
            return _busToRegisterTo.Select((keyValue) => (keyValue.Key, keyValue.Value)).ToArray();
        }

    }
}
