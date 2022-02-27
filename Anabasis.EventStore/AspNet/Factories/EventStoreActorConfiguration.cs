using Anabasis.Common;
using Anabasis.EventStore.AspNet.Factories;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;

namespace Anabasis.EventStore.AspNet.Factories
{
    public class EventStoreActorConfiguration<TAggregate> : IEventStoreActorConfiguration<TAggregate> where TAggregate : IAggregate, new()
    {
        public EventStoreActorConfiguration(IActorConfiguration actorConfiguration, Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IEventStoreCache<TAggregate>> getEventStoreCache)
        {
            ActorConfiguration = actorConfiguration;
            GetEventStoreCache = getEventStoreCache;
        }

        public IActorConfiguration ActorConfiguration { get; }

        public Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IEventStoreCache<TAggregate>> GetEventStoreCache { get; }
    }
}
