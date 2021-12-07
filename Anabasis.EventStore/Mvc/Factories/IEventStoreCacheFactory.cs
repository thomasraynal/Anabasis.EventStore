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
        void Add<TActor, TKey, TAggregate>(Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreCache<TKey, TAggregate>> getEventStoreCache)
            where TActor : IStatefulActor<TKey, TAggregate>
            where TAggregate : IAggregate<TKey>, new();

        Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreCache<TKey, TAggregate>> Get<TKey, TAggregate>(Type type)
            where TAggregate : IAggregate<TKey>, new();
    }
}
