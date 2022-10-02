using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.Common.HealthChecks;
using Anabasis.EventStore.AspNet.Builders;
using Anabasis.EventStore.Repository;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Anabasis.EventStore.AspNet
{
    public class World
    {

        private readonly List<(Type actorType, IActorBuilder builder)> _actorBuilders;
        private readonly IServiceCollection _serviceCollection;
        private readonly IActorConfigurationFactory _actorConfigurationFactory;
        private readonly bool _useEventStore;

        public World(IServiceCollection services, bool useEventStore)
        {
            _useEventStore = useEventStore;

            _actorBuilders = new List<(Type actorType, IActorBuilder builder)>();

            _serviceCollection = services;

            _actorConfigurationFactory = new ActorConfigurationFactory();

            _serviceCollection.AddSingleton<IDynamicHealthCheckProvider, DynamicHealthCheckProvider>();
            _serviceCollection.AddSingleton(_actorConfigurationFactory);
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

            _actorConfigurationFactory.AddActorConfiguration<TActor>(actorConfiguration);

            var statelessActorBuilder = new StatelessActorBuilder<TActor>(this);

            _serviceCollection.AddSingleton<TActor>();

            return statelessActorBuilder;

        }

        public EventStoreStatefulActorBuilder<TActor, TAggregate, TAggregateCacheConfiguration> AddEventStoreStatefulActor<TActor, TAggregate, TAggregateCacheConfiguration>(IEventTypeProvider eventTypeProvider, Action<IActorConfiguration>? getActorConfiguration = null, Action<TAggregateCacheConfiguration>? getAggregateCacheConfiguration = null)
            where TActor : class, IStatefulActor<TAggregate, TAggregateCacheConfiguration>
            where TAggregateCacheConfiguration : IAggregateCacheConfiguration, new()
            where TAggregate : class, IAggregate, new()
        {
                
            EnsureIsEventStoreWorld();

            EnsureActorNotAlreadyRegistered<TActor>();

            getActorConfiguration ??= new Action<IActorConfiguration>((_) => { });
            getAggregateCacheConfiguration ??= new Action<TAggregateCacheConfiguration>((_) => { });

            _serviceCollection.AddTransient<IEventStoreAggregateRepository, EventStoreAggregateRepository>();
            _serviceCollection.AddSingleton<TActor>();

            var actorConfiguration = ActorConfiguration.Default;
            var aggregateCacheConfiguration = new TAggregateCacheConfiguration();

            getActorConfiguration(actorConfiguration);
            getAggregateCacheConfiguration(aggregateCacheConfiguration);

            _actorConfigurationFactory.AddActorConfiguration<TActor>(actorConfiguration);
            _actorConfigurationFactory.AddAggregateCacheConfiguration<TActor, TAggregateCacheConfiguration, TAggregate>(aggregateCacheConfiguration);
            _actorConfigurationFactory.AddEventTypeProvider<TActor>(eventTypeProvider);

            var statelessActorBuilder = new EventStoreStatefulActorBuilder<TActor, TAggregate, TAggregateCacheConfiguration>(this);

            return statelessActorBuilder;
        }

    }
}
