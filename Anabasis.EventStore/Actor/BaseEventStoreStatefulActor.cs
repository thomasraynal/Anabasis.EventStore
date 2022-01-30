using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Mvc.Factories;
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

        public BaseEventStoreStatefulActor(IEventStoreActorConfigurationFactory eventStoreCacheFactory, IEventStoreAggregateRepository eventStoreRepository, IConnectionStatusMonitor connectionStatusMonitor, ILoggerFactory loggerFactory =null) : base(eventStoreCacheFactory, eventStoreRepository, loggerFactory)
        {
            var eventStoreActorConfiguration = eventStoreCacheFactory.GetConfiguration<TAggregate>(GetType());
            var eventStoreCache = eventStoreActorConfiguration.GetEventStoreCache(connectionStatusMonitor, loggerFactory);

            Setup(eventStoreCache);
        }

        private void Setup(IEventStoreCache<TAggregate> eventStoreCache)
        {

            State = eventStoreCache;

            eventStoreCache.Connect();
        }

        public IEventStoreCache<TAggregate> State { get; internal set; }


    }
}
