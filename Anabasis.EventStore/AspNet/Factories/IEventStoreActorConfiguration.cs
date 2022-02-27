using Anabasis.Common;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;

namespace Anabasis.EventStore.AspNet.Factories
{
    public interface IEventStoreActorConfiguration<TAggregate> where TAggregate : IAggregate, new()
    {
        Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IEventStoreCache<TAggregate>> GetEventStoreCache { get; }
    }
}
