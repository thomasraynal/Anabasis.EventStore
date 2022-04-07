using Anabasis.Common;
using Anabasis.EventStore.Factories;
using Anabasis.EventStore.Snapshot;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;

namespace Anabasis.EventStore.Actor
{
    public abstract class BaseEventStoreStatefulActor<TAggregate> : BaseStatelessActor, IStatefulActor<TAggregate> where TAggregate : IAggregate, new()
    {

        public BaseEventStoreStatefulActor(IActorConfiguration actorConfiguration, IAggregateCache<TAggregate> eventStoreCache, ILoggerFactory? loggerFactory = null) : base(actorConfiguration,  loggerFactory)
        {
            State = eventStoreCache;

            eventStoreCache.Connect().Wait();

            AddDisposable(eventStoreCache);
        }

        public BaseEventStoreStatefulActor(IEventStoreActorConfigurationFactory eventStoreCacheFactory, 
            IConnectionStatusMonitor<IEventStoreConnection> connectionStatusMonitor,
            ISnapshotStore<TAggregate>? snapshotStore = null,
            ISnapshotStrategy? snapshotStrategy = null,
            ILoggerFactory? loggerFactory = null) : base(eventStoreCacheFactory, loggerFactory)
        {
            var eventStoreActorConfiguration = eventStoreCacheFactory.GetConfiguration<TAggregate>(GetType());
            var eventStoreCache = eventStoreActorConfiguration.GetEventStoreCache(connectionStatusMonitor, loggerFactory, snapshotStore, snapshotStrategy);

            State = eventStoreCache;

            eventStoreCache.Connect().Wait();

            AddDisposable(eventStoreCache);
        }

        public override bool IsCaughtUp => State.IsCaughtUp;

        public IAggregateCache<TAggregate> State { get; internal set; }

    }
}
