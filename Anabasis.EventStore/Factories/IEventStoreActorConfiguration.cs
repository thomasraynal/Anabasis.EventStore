using Anabasis.Common;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Snapshot;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;

namespace Anabasis.EventStore.Factories
{
    public interface IEventStoreActorConfiguration<TAggregate> where TAggregate : IAggregate, new()
    {
        Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, ISnapshotStore<TAggregate>, ISnapshotStrategy, IAggregateCache<TAggregate>> GetEventStoreCache { get; }
    }
}
