using Anabasis.Common;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Standalone;
using EventStore.ClientAPI;
using Lamar;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Standalone
{
    public class StatelessActorBuilder<TActor, TRegistry>
      where TActor : IActor
      where TRegistry : ServiceRegistry, new()
    {

        private ILoggerFactory LoggerFactory { get; set; }
        private IConnectionStatusMonitor<IEventStoreConnection> ConnectionMonitor { get; set; }
        private IActorConfiguration ActorConfiguration { get; set; }

        private readonly Dictionary<Type, Action<Container, IActor>> _busToRegisterTo;

        private StatelessActorBuilder()
        {
            _busToRegisterTo = new Dictionary<Type, Action<Container, IActor>>();
        }

        public static StatelessActorBuilder<TActor, TRegistry> Create(
            IActorConfiguration actorConfiguration,
            ILoggerFactory loggerFactory = null)
        {

            loggerFactory ??= new DummyLoggerFactory();

            var builder = new StatelessActorBuilder<TActor, TRegistry>
            {
                LoggerFactory = loggerFactory,
                ActorConfiguration = actorConfiguration
            };

            return builder;

        }

        public TActor Build()
        {
            var container = new Container(configuration =>
            {
                configuration.For<IActorConfiguration>().Use(ActorConfiguration);
                configuration.For<ILoggerFactory>().Use(LoggerFactory);
                configuration.For<IConnectionStatusMonitor<IEventStoreConnection>>().Use(ConnectionMonitor);
                configuration.IncludeRegistry<TRegistry>();
            });

            var actor = container.GetInstance<TActor>();

            foreach (var busRegistration in _busToRegisterTo)
            {
                var bus = (IBus)container.GetInstance(busRegistration.Key);

                bus.Initialize().Wait();
                actor.ConnectTo(bus).Wait();

                var onBusRegistration = busRegistration.Value;

                onBusRegistration(container, actor);

            }

            return actor;

        }

        public StatelessActorBuilder<TActor, TRegistry> WithBus<TBus>(Action<TActor, TBus> onStartup=null) where TBus : IBus
        {
            var busType = typeof(TBus);

            onStartup ??= new Action<TActor, TBus>((actor, bus) => { });

            if (_busToRegisterTo.ContainsKey(busType))
                throw new InvalidOperationException($"ActorBuilder already has a reference to a bus of type {busType}");

            var onRegistration = new Action<Container, IActor>((container, actor) =>
            {
                var bus = container.GetInstance<TBus>();

                onStartup((TActor)actor, bus);

            });

            _busToRegisterTo.Add(busType, onRegistration);

            return this;
        }
    }
}
