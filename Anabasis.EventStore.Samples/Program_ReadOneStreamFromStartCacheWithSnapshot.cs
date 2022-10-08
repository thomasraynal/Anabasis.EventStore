using Anabasis.Common;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Snapshot;
using Anabasis.EventStore.Standalone;

namespace Anabasis.EventStore.Samples
{
    public static class Program_ReadOneStreamFromStartCacheWithSnapshot
    {

        public static void Run()
        {
            var eventTypeProvider = new DefaultEventTypeProvider<EventCountAggregate>(() => new[] { typeof(EventCountOne), typeof(EventCountTwo) }); ;

            var fileSystemSnapshotProvider = new FileSystemSnapshotProvider<EventCountAggregate>();

            var defaultSnapshotStrategy = new DefaultSnapshotStrategy(5);

            var multipleStreamsCatchupCacheConfiguration = new MultipleStreamsCatchupCacheConfiguration(StaticData.EntityOne)
            {
                KeepAppliedEventsOnAggregate = true
            };

            var eventCountActorWithSnapshot = EventStoreStatefulActorBuilder<EventCountStatefulActor2, MultipleStreamsCatchupCacheConfiguration, EventCountAggregate, DemoSystemRegistry>
                                       .Create(StaticData.EventStoreUrl, Do.GetConnectionSettings(), multipleStreamsCatchupCacheConfiguration, ActorConfiguration.Default, eventTypeProvider)
                                       .Build();

            var eventCountActor = EventStoreStatefulActorBuilder<EventCountStatefulActor2, MultipleStreamsCatchupCacheConfiguration, EventCountAggregate, DemoSystemRegistry>
                           .Create(StaticData.EventStoreUrl, 
                                connectionSettings: Do.GetConnectionSettings(), 
                                aggregateCacheConfiguration: multipleStreamsCatchupCacheConfiguration, 
                                actorConfiguration: ActorConfiguration.Default, 
                                eventTypeProvider: eventTypeProvider,
                                snapshotStore: fileSystemSnapshotProvider, 
                                snapshotStrategy: defaultSnapshotStrategy)
                           .Build();

            Do.Run(eventCountActor, eventCountActorWithSnapshot);
        }

    }
}
