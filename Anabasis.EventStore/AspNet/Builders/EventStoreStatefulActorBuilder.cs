﻿using System;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Snapshot;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.EventProvider;
using System.Collections.Generic;
using Anabasis.EventStore.Stream;
using Microsoft.Extensions.Logging;
using EventStore.ClientAPI;
using Anabasis.Common;
using System.Linq;
using Anabasis.EventStore.AspNet.Factories;

namespace Anabasis.EventStore.AspNet.Builders
{

    public class EventStoreStatefulActorBuilder<TActor, TAggregate> : IEventStoreStatefulActorBuilder
        where TActor : class, IEventStoreStatefulActor<TAggregate>
        where TAggregate : class, IAggregate, new()
    {
        private readonly World _world;
        private readonly List<Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IEventStoreStream>> _streamsToRegisterTo;
        private readonly Dictionary<Type, Action<IServiceProvider, IActor>> _busToRegisterTo;
        private readonly IEventStoreActorConfigurationFactory _eventStoreCacheFactory;
        private readonly IActorConfiguration _actorConfiguration;
        private IEventStoreActorConfiguration<TAggregate> _eventStoreActorConfiguration;

        public EventStoreStatefulActorBuilder(World world, IActorConfiguration actorConfiguration, IEventStoreActorConfigurationFactory eventStoreCacheFactory)
        {
            _streamsToRegisterTo = new List<Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IEventStoreStream>>();
            _eventStoreCacheFactory = eventStoreCacheFactory;
            _actorConfiguration = actorConfiguration;
            _busToRegisterTo = new Dictionary<Type, Action<IServiceProvider, IActor>>();
            _world = world;
        }

        public World CreateActor()
        {
            _world.EventStoreStatefulActorBuilders.Add((typeof(TActor), this));
            _eventStoreCacheFactory.AddConfiguration<TActor>(_actorConfiguration);
            _eventStoreCacheFactory.AddConfiguration<TActor, TAggregate>(_eventStoreActorConfiguration);
            return _world;
        }

        public EventStoreStatefulActorBuilder<TActor, TAggregate> WithReadAllFromStartCache(
          IEventTypeProvider<TAggregate> eventTypeProvider = null,
          Action<AllStreamsCatchupCacheConfiguration<TAggregate>> catchupEventStoreCacheConfigurationBuilder = null,
          ISnapshotStore<TAggregate> snapshotStore = null,
          ISnapshotStrategy snapshotStrategy = null)
        {

            var getCatchupEventStoreStream = new Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IEventStoreCache<TAggregate>>((connectionMonitor, loggerFactory) =>
               {
                   var catchupEventStoreCacheConfiguration = new AllStreamsCatchupCacheConfiguration<TAggregate>(Position.Start);

                   catchupEventStoreCacheConfigurationBuilder?.Invoke(catchupEventStoreCacheConfiguration);

                   var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TAggregate, TActor>();

                   var catchupEventStoreCache = new AllStreamsCatchupCache<TAggregate>(connectionMonitor,
                       catchupEventStoreCacheConfiguration,
                       eventProvider,
                       loggerFactory,
                       snapshotStore,
                       snapshotStrategy);

                   return catchupEventStoreCache;

               });

            if (null != _eventStoreActorConfiguration) throw new InvalidOperationException("A cache as already been registered - only one cache allowed");

            _eventStoreActorConfiguration = new EventStoreActorConfiguration<TAggregate>(_actorConfiguration, getCatchupEventStoreStream);

            return this;

        }

        public EventStoreStatefulActorBuilder<TActor, TAggregate> WithReadOneStreamFromStartCache(
          string streamId,
          IEventTypeProvider<TAggregate> eventTypeProvider = null,
          Action<MultipleStreamsCatchupCacheConfiguration<TAggregate>> getMultipleStreamsCatchupCacheConfiguration = null,
          ISnapshotStore<TAggregate> snapshotStore = null,
          ISnapshotStrategy snapshotStrategy = null)
        {
            return WithReadManyStreamFromStartCache(new[] { streamId }, eventTypeProvider, getMultipleStreamsCatchupCacheConfiguration, snapshotStore, snapshotStrategy);
        }

        public EventStoreStatefulActorBuilder<TActor, TAggregate> WithReadManyStreamFromStartCache(
          string[] streamIds,
          IEventTypeProvider<TAggregate> eventTypeProvider = null,
          Action<MultipleStreamsCatchupCacheConfiguration<TAggregate>> getMultipleStreamsCatchupCacheConfiguration = null,
          ISnapshotStore<TAggregate> snapshotStore = null,
          ISnapshotStrategy snapshotStrategy = null)
        {

            var getSubscribeFromEndMultipleStreamsEventStoreCache = new Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IEventStoreCache<TAggregate>>((connectionMonitor, loggerFactory) =>
           {
               var multipleStreamsCatchupCacheConfiguration = new MultipleStreamsCatchupCacheConfiguration<TAggregate>(streamIds);

               getMultipleStreamsCatchupCacheConfiguration?.Invoke(multipleStreamsCatchupCacheConfiguration);

               var multipleStreamsCatchupCache = new MultipleStreamsCatchupCache<TAggregate>(connectionMonitor, multipleStreamsCatchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy);

               return multipleStreamsCatchupCache;
           });

            if (null != _eventStoreActorConfiguration) throw new InvalidOperationException("A cache as already been registered - only one cache allowed");

            _eventStoreActorConfiguration = new EventStoreActorConfiguration<TAggregate>(_actorConfiguration, getSubscribeFromEndMultipleStreamsEventStoreCache);

            return this;
        }

        public EventStoreStatefulActorBuilder<TActor, TAggregate> WithReadAllFromEndCache(
          IEventTypeProvider<TAggregate> eventTypeProvider = null,
          Action<AllStreamsCatchupCacheConfiguration<TAggregate>> volatileCacheConfigurationBuilder = null)
        {
            var getSubscribeFromEndEventStoreCache = new Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IEventStoreCache<TAggregate>>((connectionMonitor, loggerFactory) =>
           {
               var subscribeFromEndCacheConfiguration = new AllStreamsCatchupCacheConfiguration<TAggregate>(Position.End);

               volatileCacheConfigurationBuilder?.Invoke(subscribeFromEndCacheConfiguration);

               var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TAggregate, TActor>();

               var subscribeFromEndEventStoreCache = new AllStreamsCatchupCache<TAggregate>(connectionMonitor,
                   subscribeFromEndCacheConfiguration,
                   eventProvider, loggerFactory);

               return subscribeFromEndEventStoreCache;

           });

            if (null != _eventStoreActorConfiguration) throw new InvalidOperationException("A cache as already been registered - only one cache allowed");

            _eventStoreActorConfiguration = new EventStoreActorConfiguration<TAggregate>(_actorConfiguration, getSubscribeFromEndEventStoreCache);


            return this;
        }

        public EventStoreStatefulActorBuilder<TActor, TAggregate> WithSubscribeFromEndToAllStreams(
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
                  eventProvider, loggerFactory);

                return subscribeFromEndEventStoreStream;

            });

            _streamsToRegisterTo.Add(getSubscribeFromEndEventStoreStream);

            return this;
        }

        public EventStoreStatefulActorBuilder<TActor, TAggregate> WithPersistentSubscriptionStream(
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

        public EventStoreStatefulActorBuilder<TActor, TAggregate> WithSubscribeFromStartToOneStream(
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
                  eventProvider, loggerFactory);

                return subscribeFromEndToOneStreamEventStoreStream;

            });

            _streamsToRegisterTo.Add(getSubscribeFromEndToOneStreamStream);

            return this;

        }

        public EventStoreStatefulActorBuilder<TActor, TAggregate> WithSubscribeFromEndToOneStream(
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
                  eventProvider,
                  loggerFactory);

                return subscribeFromEndToOneStreamEventStoreStream;

            });

            _streamsToRegisterTo.Add(getSubscribeFromEndToOneStreamStream);

            return this;

        }

        public EventStoreStatefulActorBuilder<TActor, TAggregate> WithBus<TBus>(Action<TActor, TBus> onStartup = null) where TBus : IBus
        {
            var busType = typeof(TBus);

            onStartup ??= new Action<TActor, TBus>((actor, bus) => { });

            if (_busToRegisterTo.ContainsKey(busType))
                throw new InvalidOperationException($"ActorBuilder already has a reference to a bus of type {busType}");

            var onRegistration = new Action<IServiceProvider, IActor>((serviceProvider, actor) =>
            {
                var bus = (TBus)serviceProvider.GetService(busType);

                if (null == bus)
                    throw new InvalidOperationException($"No bus of type {busType} has been registered");

                bus.Initialize().Wait();
                actor.ConnectTo(bus).Wait();

                onStartup((TActor)actor, bus);

            });

            _busToRegisterTo.Add(busType, onRegistration);

            return this;
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