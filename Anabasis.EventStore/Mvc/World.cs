using Anabasis.EventStore.Actor;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Shared;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
    public class World
    {
        internal List<(Type actorType, IStatelessActorBuilder builder)> StatelessActorBuilders { get; set; }
        internal IServiceCollection ServiceCollection { get; }

        private readonly IEventTypeProviderFactory _eventTypeProviderFactory;

        internal World(IServiceCollection services)
        {
            StatelessActorBuilders = new List<(Type, IStatelessActorBuilder)>();
            ServiceCollection = services;

            _eventTypeProviderFactory = new EventTypeProviderFactory();

            ServiceCollection.AddSingleton(_eventTypeProviderFactory);
        }

        public StatelessActorBuilder<TActor> AddStatelessActor<TActor>(IEventTypeProvider eventTypeProvider = null)
            where TActor : class, IStatelessActor
        {

            eventTypeProvider ??= new ConsumerBasedEventProvider<TActor>();

            _eventTypeProviderFactory.Add<TActor>(eventTypeProvider);

            var statelessActorBuilder = new StatelessActorBuilder<TActor>(this);

            ServiceCollection.AddSingleton<TActor>();

            return statelessActorBuilder;

        }

        public StatefulActorBuilder<TActor, TKey, TAggregate> AddStatefulActor<TActor, TKey, TAggregate>(IEventTypeProvider eventTypeProvider = null)
            where TActor : class, IStatefulActor<TKey, TAggregate>
            where TAggregate : IAggregate<TKey>, new()
        {

            eventTypeProvider ??= new ConsumerBasedEventProvider<TActor>();

            _eventTypeProviderFactory.Add<TActor>(eventTypeProvider);

            var statefulActorBuilder = new StatefulActorBuilder<TActor, TKey, TAggregate>(this);

            ServiceCollection.AddTransient<IEventStoreAggregateRepository<TKey>, EventStoreAggregateRepository<TKey>>();
            ServiceCollection.AddSingleton<TActor>();

            return statefulActorBuilder;

        }

        internal void Add<TActor>(IStatelessActorBuilder actorBuilder) where TActor : IStatelessActor
        {
            StatelessActorBuilders.Add((typeof(TActor), actorBuilder));
        }
    }
}
