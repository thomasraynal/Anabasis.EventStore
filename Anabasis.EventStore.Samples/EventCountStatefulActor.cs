using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Snapshot;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;

namespace Anabasis.EventStore.Samples
{
    public class EventCountStatefulActor : SubscribeToAllStreamsEventStoreStatefulActor<EventCountAggregate>
    {
        public EventCountStatefulActor(IActorConfigurationFactory actorConfigurationFactory, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, ILoggerFactory loggerFactory = null, ISnapshotStore<EventCountAggregate> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, IKillSwitch killSwitch = null) : base(actorConfigurationFactory, connectionMonitor, loggerFactory, snapshotStore, snapshotStrategy, killSwitch)
        {
        }

        public EventCountStatefulActor(IActorConfiguration actorConfiguration, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, AllStreamsCatchupCacheConfiguration catchupCacheConfiguration, IEventTypeProvider eventTypeProvider, ILoggerFactory loggerFactory = null, ISnapshotStore<EventCountAggregate> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, IKillSwitch killSwitch = null) : base(actorConfiguration, connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy, killSwitch)
        {
        }
    }

    public class EventCountStatefulActor2 : SubscribeToManyStreamsEventStoreStatefulActor<EventCountAggregate>
    {
        public EventCountStatefulActor2(IActorConfigurationFactory actorConfigurationFactory, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, ILoggerFactory loggerFactory = null, ISnapshotStore<EventCountAggregate> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, IKillSwitch killSwitch = null) : base(actorConfigurationFactory, connectionMonitor, loggerFactory, snapshotStore, snapshotStrategy, killSwitch)
        {
        }

        public EventCountStatefulActor2(IActorConfiguration actorConfiguration, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, MultipleStreamsCatchupCacheConfiguration catchupCacheConfiguration, IEventTypeProvider eventTypeProvider, ILoggerFactory loggerFactory = null, ISnapshotStore<EventCountAggregate> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null) : base(actorConfiguration, connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {
        }
    }
}
