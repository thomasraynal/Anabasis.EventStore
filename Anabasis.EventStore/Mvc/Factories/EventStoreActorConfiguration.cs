using Anabasis.Common;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Microsoft.Extensions.Logging;
using System;

namespace Anabasis.EventStore.Mvc.Factories
{
    public class EventStoreActorConfiguration<TAggregate> : IEventStoreActorConfiguration<TAggregate> where TAggregate : IAggregate, new()
    {
        public EventStoreActorConfiguration(IActorConfiguration actorConfiguration, Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreCache<TAggregate>> getEventStoreCache)
        {
            ActorConfiguration = actorConfiguration;
            GetEventStoreCache = getEventStoreCache;
        }

        public IActorConfiguration ActorConfiguration { get; }

        public Func<IConnectionStatusMonitor, ILoggerFactory, Cache.IEventStoreCache<TAggregate>> GetEventStoreCache { get; }
    }
}
