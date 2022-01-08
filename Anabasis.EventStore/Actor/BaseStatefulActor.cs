using Anabasis.Common;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using Microsoft.Extensions.Logging;
using System;

namespace Anabasis.EventStore.Actor
{
    public abstract class BaseStatefulActor< TAggregate> : BaseStatelessActor, IDisposable, IStatefulActor<TAggregate> where TAggregate : IAggregate, new()
    {

        public BaseStatefulActor(IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<TAggregate> eventStoreCache, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, loggerFactory)
        {
            Setup(eventStoreCache);
        }

        public BaseStatefulActor(IEventStoreAggregateRepository eventStoreRepository, IConnectionStatusMonitor connectionStatusMonitor, IEventStoreCacheFactory eventStoreCacheFactory, ILoggerFactory loggerFactory =null) : base(eventStoreRepository, loggerFactory)
        {
            var getEventStoreCache = eventStoreCacheFactory.Get< TAggregate>(GetType());

            Setup(getEventStoreCache(connectionStatusMonitor, loggerFactory));
        }

        private void Setup(IEventStoreCache<TAggregate> eventStoreCache)
        {

            State = eventStoreCache;

            eventStoreCache.Connect();
        }

        public IEventStoreCache<TAggregate> State { get; internal set; }


    }
}
