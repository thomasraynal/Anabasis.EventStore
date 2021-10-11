using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Actor
{
    public interface IEventStoreCacheFactory
    {
        IEventStoreCache<TKey, TAggregate> Get<TKey, TAggregate>() where TAggregate : IAggregate<TKey>, new();
    }
}
