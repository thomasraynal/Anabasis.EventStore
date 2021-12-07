using Anabasis.Common;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Anabasis.EventStore.Mvc
{
    public class EventStoreCacheFactory : IEventStoreCacheFactory
    {
        private readonly Dictionary<Type, object> _eventStoreCaches;

        public EventStoreCacheFactory()
        {
            _eventStoreCaches = new Dictionary<Type, object>();
        }

        public void Add<TActor, TKey, TAggregate>(Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreCache<TKey, TAggregate>> getEventStoreCache)
            where TActor : IStatefulActor<TKey, TAggregate>
            where TAggregate : IAggregate<TKey>, new()
        {
            _eventStoreCaches.Add(typeof(TActor), getEventStoreCache);
        }

        public Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreCache<TKey, TAggregate>> Get<TKey, TAggregate>(Type type)
            where TAggregate : IAggregate<TKey>, new()
        {
            return (Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreCache <TKey, TAggregate>>)_eventStoreCaches[type];
        }
    }
}
