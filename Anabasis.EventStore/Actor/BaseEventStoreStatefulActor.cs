using Anabasis.Common;
using Anabasis.EventStore.AspNet.Factories;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;

namespace Anabasis.EventStore.Actor
{
    public abstract class BaseEventStoreStatefulActor<TAggregate> : BaseEventStoreStatelessActor, IEventStoreStatefulActor<TAggregate> where TAggregate : IAggregate, new()
    {

        public BaseEventStoreStatefulActor(IActorConfiguration actorConfiguration, IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<TAggregate> eventStoreCache, IConnectionStatusMonitor<IEventStoreConnection> connectionStatusMonitor, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, connectionStatusMonitor, loggerFactory)
        {
            Initialize(eventStoreCache);
        }

        public BaseEventStoreStatefulActor(IEventStoreActorConfigurationFactory eventStoreCacheFactory, IEventStoreAggregateRepository eventStoreRepository, IConnectionStatusMonitor<IEventStoreConnection> connectionStatusMonitor, ILoggerFactory loggerFactory = null) : base(eventStoreCacheFactory, eventStoreRepository, connectionStatusMonitor, loggerFactory)
        {
            var eventStoreActorConfiguration = eventStoreCacheFactory.GetConfiguration<TAggregate>(GetType());
            var eventStoreCache = eventStoreActorConfiguration.GetEventStoreCache(connectionStatusMonitor, loggerFactory);

            Initialize(eventStoreCache);
        }

        private void Initialize(IEventStoreCache<TAggregate> eventStoreCache)
        {

            State = eventStoreCache;

            eventStoreCache.Connect().Wait();

            AddDisposable(eventStoreCache);
        }

        public IEventStoreCache<TAggregate> State { get; internal set; }


    }
}
