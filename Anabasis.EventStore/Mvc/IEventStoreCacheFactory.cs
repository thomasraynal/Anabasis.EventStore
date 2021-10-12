using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Actor
{
    public interface IEventStoreCacheFactory
    {
        void Add<TActor, TKey, TAggregate>(Func<IConnectionStatusMonitor, IEventStoreCache<TKey, TAggregate>> getEventStoreCache)
            where TActor : IStatefulActor<TKey, TAggregate>
            where TAggregate : IAggregate<TKey>, new();

        Func<IConnectionStatusMonitor, IEventStoreCache<TKey, TAggregate>> Get<TKey, TAggregate>(Type type)
            where TAggregate : IAggregate<TKey>, new();
    }
}
