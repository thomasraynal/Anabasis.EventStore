﻿using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.EventStore.AspNet.Builders
{
    public class StatelessActorBuilder<TActor> : IStatelessActorBuilder
        where TActor : IActor
    {
        private readonly World _world;
        private readonly Dictionary<Type, Action<IServiceProvider, IActor>> _busToRegisterTo;

        public StatelessActorBuilder(World world)
        {
            _world = world;
            _busToRegisterTo = new Dictionary<Type, Action<IServiceProvider, IActor>>();
        }

        public StatelessActorBuilder<TActor> WithBus<TBus>(Action<TActor, TBus> onStartup=null) where TBus : IBus
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

        public World CreateActor()
        {
            _world.StatelessActorBuilders.Add((typeof(TActor), this));
            return _world;
        }

        public (Type actor, Action<IServiceProvider, IActor> factory)[] GetBusFactories()
        {
            return _busToRegisterTo.Select((keyValue) => (keyValue.Key, keyValue.Value)).ToArray();
        }

    }
}