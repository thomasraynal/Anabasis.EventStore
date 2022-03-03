using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.Common.HealthChecks;
using Anabasis.EventStore.AspNet.Builders;
using Anabasis.EventStore.AspNet.Factories;
using Anabasis.EventStore.Repository;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Anabasis.EventStore
{
    public class World
    {

        private readonly List<(Type actorType, IActorBuilder builder)> _actorBuilders;
        private readonly IServiceCollection _serviceCollection;
        private readonly IEventStoreActorConfigurationFactory _eventStoreCacheFactory;
        private readonly bool _useEventStore;

        public World(IServiceCollection services, bool useEventStore)
        {
            _useEventStore = useEventStore;

            _actorBuilders = new List<(Type actorType, IActorBuilder builder)>();

            _serviceCollection = services;

            _eventStoreCacheFactory = new EventStoreCacheFactory();

            _serviceCollection.AddSingleton<IDynamicHealthCheckProvider, DynamicHealthCheckProvider>();
            _serviceCollection.AddSingleton<IActorConfigurationFactory>(_eventStoreCacheFactory);
            _serviceCollection.AddSingleton(_eventStoreCacheFactory);
        }

        public (Type actorType,IActorBuilder actorBuilder)[] GetBuilders()
        {
            return _actorBuilders.ToArray();
        }

        public void AddBuilder<TActor>(IActorBuilder builder) where TActor : IActor
        {
            _actorBuilders.Add((typeof(TActor), builder));
        }

        private void EnsureIsEventStoreWorld()
        {
            if (!_useEventStore)
                throw new InvalidOperationException("This world does not support eventstore - use another world builder method");
        }

        private void EnsureActorNotAlreadyRegistered<TActor>()
        {
            if (_actorBuilders.Any(statefulActorBuilder => statefulActorBuilder.actorType == typeof(TActor)))
            {
                throw new InvalidOperationException($"Actor {typeof(TActor)} has already been registered. Actors are registered as singleton in the AspNetCore.Builder context : only one instance of each actor type is authorized." +
                         $" Use the Anabasis.EventStore.Actor.*Builders and register/invoke them manually if you wish create multiples actors of the same type");
            }
        }

        public StatelessActorBuilder<TActor> AddStatelessActor<TActor>(IActorConfiguration actorConfiguration)
            where TActor : class, IActor
        {

            EnsureActorNotAlreadyRegistered<TActor>();

            _eventStoreCacheFactory.AddConfiguration<TActor>(actorConfiguration);

            var statelessActorBuilder = new StatelessActorBuilder<TActor>(this);

            _serviceCollection.AddSingleton<TActor>();

            return statelessActorBuilder;

        }

        public EventStoreStatefulActorBuilder<TActor, TAggregate> AddEventStoreStatefulActor<TActor, TAggregate>(IActorConfiguration actorConfiguration)
            where TActor : class, IStatefulActor<TAggregate>
            where TAggregate : class, IAggregate, new()
        {

            EnsureIsEventStoreWorld();

            EnsureActorNotAlreadyRegistered<TActor>();

            _serviceCollection.AddTransient<IEventStoreAggregateRepository, EventStoreAggregateRepository>();
            _serviceCollection.AddSingleton<TActor>();

            var statelessActorBuilder = new EventStoreStatefulActorBuilder<TActor, TAggregate>(this, actorConfiguration, _eventStoreCacheFactory);

            return statelessActorBuilder;
        }

    }
}
