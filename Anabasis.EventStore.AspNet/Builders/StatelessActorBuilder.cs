using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.EventStore.AspNet.Builders
{
    public class StatelessActorBuilder<TActor> : IActorBuilder
        where TActor : IAnabasisActor
    {
        private readonly World _world;
        private readonly Dictionary<Type, Action<IServiceProvider, IAnabasisActor>> _busToRegisterTo;

        public StatelessActorBuilder(World world)
        {
            _world = world;
            _busToRegisterTo = new Dictionary<Type, Action<IServiceProvider, IAnabasisActor>>();
        }

        public StatelessActorBuilder<TActor> WithBus<TBus>(Action<TActor, TBus>? onStartup = null) where TBus : IBus
        {
            var busType = typeof(TBus);

            onStartup ??= new Action<TActor, TBus>((actor, bus) => { });

            if (_busToRegisterTo.ContainsKey(busType))
                throw new InvalidOperationException($"ActorBuilder already has a reference to a bus of type {busType}");

            var onRegistration = new Action<IServiceProvider, IAnabasisActor>((serviceProvider, actor) =>
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

        public World CreateActor()
        {
            _world.AddBuilder<TActor>(this);
            return _world;
        }

        public (Type actor, Action<IServiceProvider, IAnabasisActor> factory)[] GetBusFactories()
        {
            return _busToRegisterTo.Select((keyValue) => (keyValue.Key, keyValue.Value)).ToArray();
        }

    }
}
