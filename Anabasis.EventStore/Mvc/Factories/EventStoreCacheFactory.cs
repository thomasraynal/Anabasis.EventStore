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

        public void Add<TActor,  TAggregate>(Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreCache< TAggregate>> getEventStoreCache)
            where TActor : IStatefulActor< TAggregate>
            where TAggregate : IAggregate, new()
        {
            _eventStoreCaches.Add(typeof(TActor), getEventStoreCache);
        }

        public Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreCache< TAggregate>> Get< TAggregate>(Type type)
            where TAggregate : IAggregate, new()
        {
            return (Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreCache < TAggregate>>)_eventStoreCaches[type];
        }
    }
}
