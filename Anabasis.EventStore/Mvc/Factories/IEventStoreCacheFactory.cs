using Anabasis.Common;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Shared;
using Microsoft.Extensions.Logging;
using System;

namespace Anabasis.EventStore.Actor
{
    public interface IEventStoreCacheFactory
    {
        void Add<TActor,  TAggregate>(Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreCache< TAggregate>> getEventStoreCache)
            where TActor : IStatefulActor< TAggregate>
            where TAggregate : IAggregate, new();

        Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreCache< TAggregate>> Get< TAggregate>(Type type)
            where TAggregate : IAggregate, new();
    }
}
