using Anabasis.Common;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Microsoft.Extensions.Logging;
using System;

namespace Anabasis.EventStore.Mvc.Factories
{
    public interface IEventStoreActorConfiguration<TAggregate> where TAggregate : IAggregate, new()
    {
        Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreCache<TAggregate>> GetEventStoreCache { get; }
    }
}
