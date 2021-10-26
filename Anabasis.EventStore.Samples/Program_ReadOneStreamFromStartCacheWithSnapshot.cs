using Anabasis.EventStore.Actor;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Snapshot;
using Anabasis.EventStore.Standalone;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Lamar;
using System;

namespace Anabasis.EventStore.Samples
{
    public static class Program_ReadOneStreamFromStartCacheWithSnapshot
    {

        public static void Run()
        {
            var eventTypeProvider = new DefaultEventTypeProvider<string, EventCountAggregate>(() => new[] { typeof(EventCountOne), typeof(EventCountTwo) }); ;

            var fileSystemSnapshotProvider = new FileSystemSnapshotProvider<string, EventCountAggregate>();

            var defaultSnapshotStrategy = new DefaultSnapshotStrategy<string>(5);

            var eventCountActorWithSnapshot = StatefulActorBuilder<EventCountStatefulActor, string, EventCountAggregate, DemoSystemRegistry>
                                       .Create(StaticData.EventStoreUrl, Do.GetConnectionSettings())
                                       .WithReadOneStreamFromStartCache(StaticData.EntityOne,
                                            eventTypeProvider: eventTypeProvider,
                                            getMultipleStreamsCatchupCacheConfiguration: builder =>
                                            {
                                                builder.KeepAppliedEventsOnAggregate = true;
                                                builder.UseSnapshot = true;
                                            },
                                           snapshotStore: fileSystemSnapshotProvider,
                                           snapshotStrategy: defaultSnapshotStrategy)
                                       .Build();

            var eventCountActor = StatefulActorBuilder<EventCountStatefulActor, string, EventCountAggregate, DemoSystemRegistry>
                           .Create(StaticData.EventStoreUrl, Do.GetConnectionSettings())
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
