using Anabasis.EventStore.Actor;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Mvc;
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
        internal List<(Type actorType, IStatefulActorBuilder builder)> StatefulActorBuilders { get; }
        internal List<(Type actorType, IStatelessActorBuilder builder)> StatelessActorBuilders { get;  }

        internal IServiceCollection ServiceCollection { get; }

        private readonly EventStoreCacheFactory _eventStoreCacheFactory;
        private readonly IEventTypeProviderFactory _eventTypeProviderFactory;

        internal World(IServiceCollection services)
        {
            StatelessActorBuilders = new List<(Type, IStatelessActorBuilder)>();
            StatefulActorBuilders = new List<(Type actorType, IStatefulActorBuilder builder)>();
            ServiceCollection = services;

            _eventStoreCacheFactory = new EventStoreCacheFactory();
            _eventTypeProviderFactory = new EventTypeProviderFactory();

            ServiceCollection.AddSingleton<IEventStoreCacheFactory>(_eventStoreCacheFactory);
            ServiceCollection.AddSingleton<IEventTypeProviderFactory>(_eventTypeProviderFactory);
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

        public StatefulActorBuilder<TActor, TKey, TAggregate> AddStatefulActor<TActor, TKey, TAggregate>()
            where TActor : class, IStatefulActor<TKey, TAggregate>
            where TAggregate : IAggregate<TKey>, new()
        {

            ServiceCollection.AddTransient<IEventStoreAggregateRepository<TKey>, EventStoreAggregateRepository<TKey>>();
            ServiceCollection.AddSingleton<TActor>();

            var statelessActorBuilder = new StatefulActorBuilder<TActor, TKey, TAggregate>(this, _eventStoreCacheFactory);

            return statelessActorBuilder;
        }

    }
}
