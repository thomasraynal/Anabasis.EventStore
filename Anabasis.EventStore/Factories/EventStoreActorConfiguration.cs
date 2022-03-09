﻿using Anabasis.Common;
using Anabasis.EventStore.Snapshot;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;

namespace Anabasis.EventStore.Factories
{
    public class EventStoreActorConfiguration<TAggregate> : IEventStoreActorConfiguration<TAggregate> where TAggregate : IAggregate, new()
    {
        public EventStoreActorConfiguration(IActorConfiguration actorConfiguration, Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, ISnapshotStore<TAggregate>, ISnapshotStrategy, IAggregateCache<TAggregate>> getEventStoreCache)
        {
            ActorConfiguration = actorConfiguration;
            GetEventStoreCache = getEventStoreCache;
        }

        public IActorConfiguration ActorConfiguration { get; }

        public Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, ISnapshotStore<TAggregate>, ISnapshotStrategy, IAggregateCache<TAggregate>> GetEventStoreCache { get; }
    }
}
