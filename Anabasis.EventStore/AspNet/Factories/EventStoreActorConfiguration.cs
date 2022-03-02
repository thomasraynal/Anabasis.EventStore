using Anabasis.Common;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;

namespace Anabasis.EventStore.AspNet.Factories
{
    public class EventStoreActorConfiguration<TAggregate> : IEventStoreActorConfiguration<TAggregate> where TAggregate : IAggregate, new()
    {
        public EventStoreActorConfiguration(IActorConfiguration actorConfiguration, Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IAggregateCache<TAggregate>> getEventStoreCache)
        {
            ActorConfiguration = actorConfiguration;
            GetEventStoreCache = getEventStoreCache;
        }

        public IActorConfiguration ActorConfiguration { get; }

        public Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IAggregateCache<TAggregate>> GetEventStoreCache { get; }
    }
}
