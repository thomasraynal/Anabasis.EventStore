using Anabasis.Common;
using Anabasis.EventStore.Factories;
using Anabasis.EventStore.Repository;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;

namespace Anabasis.EventStore.Actor
{
    public abstract class BaseEventStoreStatefulActor<TAggregate> : BaseStatelessActor, IStatefulActor<TAggregate> where TAggregate : IAggregate, new()
    {

        public BaseEventStoreStatefulActor(IActorConfiguration actorConfiguration, IAggregateCache<TAggregate> eventStoreCache, ILoggerFactory loggerFactory = null) : base(actorConfiguration,  loggerFactory)
        {
            Initialize(eventStoreCache);
        }

        public BaseEventStoreStatefulActor(IEventStoreActorConfigurationFactory eventStoreCacheFactory, IConnectionStatusMonitor<IEventStoreConnection> connectionStatusMonitor, ILoggerFactory loggerFactory = null) : base(eventStoreCacheFactory, loggerFactory)
        {
            var eventStoreActorConfiguration = eventStoreCacheFactory.GetConfiguration<TAggregate>(GetType());
            var eventStoreCache = eventStoreActorConfiguration.GetEventStoreCache(connectionStatusMonitor, loggerFactory);

            Initialize(eventStoreCache);
        }

        private void Initialize(IAggregateCache<TAggregate> eventStoreCache)
        {

            State = eventStoreCache;

            eventStoreCache.Connect().Wait();

            AddDisposable(eventStoreCache);
        }

        public IAggregateCache<TAggregate> State { get; internal set; }


    }
}
