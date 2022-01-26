using Anabasis.Common;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using Microsoft.Extensions.Logging;
using System;

namespace Anabasis.EventStore.Actor
{
    public abstract class BaseEventStoreStatefulActor< TAggregate> : BaseEventStoreStatelessActor, IEventStoreStatefulActor<TAggregate> where TAggregate : IAggregate, new()
    {

        public BaseEventStoreStatefulActor(IActorConfiguration actorConfiguration, IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<TAggregate> eventStoreCache, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, loggerFactory)
        {
            Setup(eventStoreCache);
        }

        public BaseEventStoreStatefulActor(IActorConfiguration actorConfiguration, IEventStoreAggregateRepository eventStoreRepository, IConnectionStatusMonitor connectionStatusMonitor, IEventStoreCacheFactory eventStoreCacheFactory, ILoggerFactory loggerFactory =null) : base(actorConfiguration, eventStoreRepository, loggerFactory)
        {
            var getEventStoreCache = eventStoreCacheFactory.Get<TAggregate>(GetType());

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
