using Anabasis.Common;
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

            var eventCountActorWithSnapshot = EventStoreStatefulActorBuilder<EventCountStatefulActor, EventCountAggregate, DemoSystemRegistry>
                                       .Create(StaticData.EventStoreUrl, Do.GetConnectionSettings(), ActorConfiguration.Default)
                                       .WithReadOneStreamFromStartCache(StaticData.EntityOne,
                                            eventTypeProvider: eventTypeProvider,
                                            getMultipleStreamsCatchupCacheConfiguration: builder =>
                                            {
                                                builder.KeepAppliedEventsOnAggregate = true;
                                            },
                                           snapshotStore: fileSystemSnapshotProvider,
                                           snapshotStrategy: defaultSnapshotStrategy)
                                       .Build();

            var eventCountActor = EventStoreStatefulActorBuilder<EventCountStatefulActor, EventCountAggregate, DemoSystemRegistry>
                           .Create(StaticData.EventStoreUrl, Do.GetConnectionSettings(), ActorConfiguration.Default)
                           .WithReadOneStreamFromStartCache(StaticData.EntityOne,
                                eventTypeProvider: eventTypeProvider,
                                getMultipleStreamsCatchupCacheConfiguration: builder =>
                                {
                                    builder.KeepAppliedEventsOnAggregate = true;
                                })
                           .Build();

            Do.Run(eventCountActor, eventCountActorWithSnapshot);
        }

    }
}
