using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.Common.HealthChecks;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Mvc;
using Anabasis.EventStore.Mvc.Factories;
using Anabasis.EventStore.Repository;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Anabasis.EventStore
{
    public class World
    {
        internal List<(Type actorType, IStatefulActorBuilder builder)> StatefulActorBuilders { get; }
        internal List<(Type actorType, IStatelessActorBuilder builder)> StatelessActorBuilders { get;  }

        internal IServiceCollection ServiceCollection { get; }

        private readonly IEventStoreActorConfigurationFactory _eventStoreCacheFactory;

        internal World(IServiceCollection services)
        {
            StatelessActorBuilders = new List<(Type, IStatelessActorBuilder)>();
            StatefulActorBuilders = new List<(Type actorType, IStatefulActorBuilder builder)>();
            ServiceCollection = services;

            _eventStoreCacheFactory = new EventStoreCacheFactory();

            ServiceCollection.AddSingleton<IDynamicHealthCheckProvider,DynamicHealthCheckProvider>();

            ServiceCollection.AddSingleton<IActorConfigurationFactory>(_eventStoreCacheFactory);
            ServiceCollection.AddSingleton(_eventStoreCacheFactory);
        }

        public EventStoreStatelessActorBuilder<TActor> AddStatelessActor<TActor>(IActorConfiguration actorConfiguration, IEventTypeProvider eventTypeProvider = null)
            where TActor : class, IEventStoreStatelessActor
        {
            if (StatefulActorBuilders.Any(statefulActorBuilder => statefulActorBuilder.actorType == typeof(TActor)))
                throw new InvalidOperationException($"Actors are registered as singleton in the AspNetCore.Builder context : only one instance of each actor type is authorized." +
                    $" Use the Anabasis.EventStore.Actor.*Builders and register/invoke them manually if you wish create multiples actors of the same type");

            eventTypeProvider ??= new ConsumerBasedEventProvider<TActor>();

            _eventStoreCacheFactory.AddConfiguration<TActor>(actorConfiguration);

              var statelessActorBuilder = new EventStoreStatelessActorBuilder<TActor>(this);

            ServiceCollection.AddSingleton<TActor>();

            return statelessActorBuilder;

        }

        public EventStoreStatefulActorBuilder<TActor,  TAggregate> AddStatefulActor<TActor,  TAggregate>(IActorConfiguration actorConfiguration)
            where TActor : class, IEventStoreStatefulActor< TAggregate>
            where TAggregate : IAggregate, new()
        {

            if (StatelessActorBuilders.Any(statefulActorBuilder => statefulActorBuilder.actorType == typeof(TActor)))
                throw new InvalidOperationException($"Actors are registered as singleton in the AspNetCore.Builder context : only one instance of each actor type is authorized." +
                    $" Use the Anabasis.EventStore.Actor.*Builders and register/invoke them manually if you wish create multiples actors of the same type");

            ServiceCollection.AddTransient<IEventStoreAggregateRepository, EventStoreAggregateRepository>();
            ServiceCollection.AddSingleton<TActor>();
    
            var statelessActorBuilder = new EventStoreStatefulActorBuilder<TActor,  TAggregate>(this, actorConfiguration, _eventStoreCacheFactory);

            return statelessActorBuilder;
        }

    }
}
