using System;
using System.Collections.Generic;
using Anabasis.Common;
using System.Linq;

namespace Anabasis.EventStore.AspNet.Builders
{

    public class EventStoreStatefulActorBuilder<TActor, TAggregate, TAggregateCacheConfiguration> : IActorBuilder
        where TActor : class, IStatefulActor<TAggregate, TAggregateCacheConfiguration>
        where TAggregateCacheConfiguration : IAggregateCacheConfiguration
        where TAggregate : class, IAggregate, new()
    {
        private readonly World _world;
        private readonly Dictionary<Type, Action<IServiceProvider, IAnabasisActor>> _busToRegisterTo;

        public EventStoreStatefulActorBuilder(World world)
        {
            _busToRegisterTo = new Dictionary<Type, Action<IServiceProvider, IAnabasisActor>>();
            _world = world;
        }

        public World CreateActor()
        {
            _world.AddBuilder<TActor>(this);
            return _world;
        }
        public EventStoreStatefulActorBuilder<TActor,TAggregate, TAggregateCacheConfiguration> WithBus<TBus>(Action<TActor, TBus>? onStartup = null) where TBus : IBus
        {
            var busType = typeof(TBus);

            onStartup ??= new Action<TActor, TBus>((actor, bus) => { });

            if (_busToRegisterTo.ContainsKey(busType))
                throw new InvalidOperationException($"ActorBuilder already has a reference to a bus of type {busType}");

            var onRegistration = new Action<IServiceProvider, IAnabasisActor>((serviceProvider, actor) =>
            {

                var busCandidate = serviceProvider.GetService(busType);

                if (null == busCandidate)
                    throw new InvalidOperationException($"No bus of type {busType} has been registered");

                var bus = (TBus)busCandidate;

                if (null == bus)
                    throw new InvalidOperationException($"No bus of type {busType} has been registered");

                actor.ConnectTo(bus).Wait();

                onStartup((TActor)actor, bus);

            });

            _busToRegisterTo.Add(busType, onRegistration);

            return this;
        }


        public (Type actor, Action<IServiceProvider, IAnabasisActor> factory)[] GetBusFactories()
        {
            return _busToRegisterTo.Select((keyValue) => (keyValue.Key, keyValue.Value)).ToArray();
        }
    }


}
