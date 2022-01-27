using Anabasis.Common;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Mvc;
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

        private readonly IEventStoreCacheFactory _eventStoreCacheFactory;

        internal World(IServiceCollection services)
        {
            StatelessActorBuilders = new List<(Type, IStatelessActorBuilder)>();
            StatefulActorBuilders = new List<(Type actorType, IStatefulActorBuilder builder)>();
            ServiceCollection = services;

            _eventStoreCacheFactory = new EventStoreCacheFactory();

            ServiceCollection.AddSingleton(_eventStoreCacheFactory);
        }

        public StatelessActorBuilder<TActor> AddStatelessActor<TActor>(IActorConfiguration actorConfiguration, IEventTypeProvider eventTypeProvider = null)
            where TActor : class, IEventStoreStatelessActor
        {
            if (StatefulActorBuilders.Any(statefulActorBuilder => statefulActorBuilder.actorType == typeof(TActor)))
                throw new InvalidOperationException($"Actors are registered as singleton in the AspNetCore.Builder context : only one instance of each actor type is authorized." +
                    $" Use the Anabasis.EventStore.Actor.*Builders and register/invoke them manually if you wish create multiples actors of the same type");

            eventTypeProvider ??= new ConsumerBasedEventProvider<TActor>();

            ServiceCollection.AddSingleton(actorConfiguration);

            var statelessActorBuilder = new StatelessActorBuilder<TActor>(this);

            ServiceCollection.AddSingleton<TActor>();

            return statelessActorBuilder;

        }

        public StatefulActorBuilder<TActor,  TAggregate> AddStatefulActor<TActor,  TAggregate>(IActorConfiguration actorConfiguration)
            where TActor : class, IEventStoreStatefulActor< TAggregate>
            where TAggregate : IAggregate, new()
        {

            if (StatelessActorBuilders.Any(statefulActorBuilder => statefulActorBuilder.actorType == typeof(TActor)))
                throw new InvalidOperationException($"Actors are registered as singleton in the AspNetCore.Builder context : only one instance of each actor type is authorized." +
                    $" Use the Anabasis.EventStore.Actor.*Builders and register/invoke them manually if you wish create multiples actors of the same type");

            ServiceCollection.AddTransient<IEventStoreAggregateRepository, EventStoreAggregateRepository>();
            ServiceCollection.AddSingleton<TActor>();
            ServiceCollection.AddSingleton(actorConfiguration);

            var statelessActorBuilder = new StatefulActorBuilder<TActor,  TAggregate>(this, _eventStoreCacheFactory);

            return statelessActorBuilder;
        }

    }
}
