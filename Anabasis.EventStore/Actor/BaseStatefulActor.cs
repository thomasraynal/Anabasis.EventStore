using Anabasis.Common;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Shared;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Actor
{
    public abstract class BaseStatefulActor<TKey, TAggregate> : BaseStatelessActor, IDisposable, IStatefulActor<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
    {

        public BaseStatefulActor(IEventStoreAggregateRepository<TKey> eventStoreRepository, IEventStoreCache<TKey, TAggregate> eventStoreCache, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, loggerFactory)
        {
            Setup(eventStoreRepository, eventStoreCache);
        }

        public BaseStatefulActor(IEventStoreAggregateRepository<TKey> eventStoreRepository, IConnectionStatusMonitor connectionStatusMonitor, IEventStoreCacheFactory eventStoreCacheFactory, ILoggerFactory loggerFactory =null) : base(eventStoreRepository, loggerFactory)
        {
            var getEventStoreCache = eventStoreCacheFactory.Get<TKey, TAggregate>(GetType());

            Setup(eventStoreRepository, getEventStoreCache(connectionStatusMonitor, loggerFactory));
        }

        private void Setup(IEventStoreAggregateRepository<TKey> eventStoreRepository, IEventStoreCache<TKey, TAggregate> eventStoreCache)
        {

            State = eventStoreCache;

            eventStoreCache.Connect();
        }

        public IEventStoreCache<TKey, TAggregate> State { get; internal set; }


    }
}
