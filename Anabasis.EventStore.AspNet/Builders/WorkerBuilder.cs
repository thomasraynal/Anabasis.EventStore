using Anabasis.Common;
using Anabasis.Common.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventStore.AspNet.Builders
{
    public class WorkerBuilder<TWorker> : IWorkerBuilder where TWorker : IWorker
    {
        private readonly World _world;
        private readonly Dictionary<Type, Action<IServiceProvider, IWorker>> _busToRegisterTo;

        public WorkerBuilder(World world)
        {
            _world = world;
            _busToRegisterTo = new Dictionary<Type, Action<IServiceProvider, IWorker>>();
        }

        public WorkerBuilder<TWorker> WithBus<TBus>(Action<TWorker, TBus>? onStartup = null) where TBus : IBus
        {
            var busType = typeof(TBus);

            onStartup ??= new Action<TWorker, TBus>((actor, bus) => { });

            if (_busToRegisterTo.ContainsKey(busType))
                throw new InvalidOperationException($"WorkerBuilder already has a reference to a bus of type {busType}");

            var onRegistration = new Action<IServiceProvider, IWorker>((serviceProvider, actor) =>
            {
                var bus = (TBus)serviceProvider.GetService(busType);

                if (null == bus)
                    throw new InvalidOperationException($"No bus of type {busType} has been registered");

                actor.ConnectTo(bus).Wait();

                onStartup((TWorker)actor, bus);

            });

            _busToRegisterTo.Add(busType, onRegistration);

            return this;
        }

        public World CreateWorker()
        {
            _world.AddBuilder<TWorker>(this);
            return _world;
        }

        public (Type workerType, Action<IServiceProvider, IWorker> factory)[] GetBusFactories()
        {
            return _busToRegisterTo.Select((keyValue) => (keyValue.Key, keyValue.Value)).ToArray();
        }
    }
}
