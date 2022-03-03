using System;
using Anabasis.EventStore.Snapshot;
using Anabasis.EventStore.Cache;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using EventStore.ClientAPI;
using Anabasis.Common;
using System.Linq;
using Anabasis.EventStore.Factories;

namespace Anabasis.EventStore.AspNet.Builders
{

    public class EventStoreStatefulActorBuilder<TActor, TAggregate> : IEventStoreStatefulActorBuilder
        where TActor : class, IStatefulActor<TAggregate>
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
            _world.AddBuilder<TActor>(this);
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

            var getCatchupEventStoreStream = new Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IAggregateCache<TAggregate>>((connectionMonitor, loggerFactory) =>
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

            var getSubscribeFromEndMultipleStreamsEventStoreCache = new Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IAggregateCache<TAggregate>>((connectionMonitor, loggerFactory) =>
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
            var getSubscribeFromEndEventStoreCache = new Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IAggregateCache<TAggregate>>((connectionMonitor, loggerFactory) =>
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
