﻿using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.Common.HealthChecks;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Mvc;
using Anabasis.EventStore.Mvc.Builders;
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
        internal List<(Type actorType, IEventStoreStatefulActorBuilder builder)> EventStoreStatefulActorBuilders { get; }
        internal List<(Type actorType, IEventStoreStatelessActorBuilder builder)> EventStoreStatelessActorBuilders { get; }
        internal List<(Type actorType, IStatelessActorBuilder builder)> StatelessActorBuilders { get; }

        internal IServiceCollection ServiceCollection { get; }

        private readonly IEventStoreActorConfigurationFactory _eventStoreCacheFactory;

        internal World(IServiceCollection services)
        {
            EventStoreStatelessActorBuilders = new List<(Type, IEventStoreStatelessActorBuilder)>();
            EventStoreStatefulActorBuilders = new List<(Type actorType, IEventStoreStatefulActorBuilder builder)>();
            StatelessActorBuilders = new List<(Type actorType, IStatelessActorBuilder builder)>();

            ServiceCollection = services;

            _eventStoreCacheFactory = new EventStoreCacheFactory();

            ServiceCollection.AddSingleton<IDynamicHealthCheckProvider, DynamicHealthCheckProvider>();
            ServiceCollection.AddSingleton<IActorConfigurationFactory>(_eventStoreCacheFactory);
            ServiceCollection.AddSingleton(_eventStoreCacheFactory);
        }

        public void EnsureActorNotAlreadyRegistered<TActor>()
        {
            if (StatelessActorBuilders.Any(statefulActorBuilder => statefulActorBuilder.actorType == typeof(TActor)) ||
            EventStoreStatefulActorBuilders.Any(eventStoreStatefulActorBuilder => eventStoreStatefulActorBuilder.actorType == typeof(TActor)) ||
            EventStoreStatelessActorBuilders.Any(eventStoreStatelessActorBuilder => eventStoreStatelessActorBuilder.actorType == typeof(TActor)))
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

            ServiceCollection.AddSingleton<TActor>();

            return statelessActorBuilder;

        }

        public EventStoreStatelessActorBuilder<TActor> AddEventStoreStatelessActor<TActor>(IActorConfiguration actorConfiguration)
            where TActor : class, IEventStoreStatelessActor
        {
            EnsureActorNotAlreadyRegistered<TActor>();

            _eventStoreCacheFactory.AddConfiguration<TActor>(actorConfiguration);

            var statelessActorBuilder = new EventStoreStatelessActorBuilder<TActor>(this);

            ServiceCollection.AddSingleton<TActor>();

            return statelessActorBuilder;

        }

        public EventStoreStatefulActorBuilder<TActor, TAggregate> AddEventStoreStatefulActor<TActor, TAggregate>(IActorConfiguration actorConfiguration)
            where TActor : class, IEventStoreStatefulActor<TAggregate>
            where TAggregate : IAggregate, new()
        {

            EnsureActorNotAlreadyRegistered<TActor>();

            ServiceCollection.AddTransient<IEventStoreAggregateRepository, EventStoreAggregateRepository>();
            ServiceCollection.AddSingleton<TActor>();

            var statelessActorBuilder = new EventStoreStatefulActorBuilder<TActor, TAggregate>(this, actorConfiguration, _eventStoreCacheFactory);

            return statelessActorBuilder;
        }

    }
}
