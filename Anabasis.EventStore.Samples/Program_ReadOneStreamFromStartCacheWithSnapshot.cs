﻿using Anabasis.EventStore.Actor;
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
            var eventTypeProvider = new DefaultEventTypeProvider<EventCountAggregate>(() => new[] { typeof(EventCountOne), typeof(EventCountTwo) }); ;

            var fileSystemSnapshotProvider = new FileSystemSnapshotProvider<EventCountAggregate>();

            var defaultSnapshotStrategy = new DefaultSnapshotStrategy(5);

            var eventCountActorWithSnapshot = StatefulActorBuilder<EventCountStatefulActor, EventCountAggregate, DemoSystemRegistry>
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

            var eventCountActor = StatefulActorBuilder<EventCountStatefulActor, EventCountAggregate, DemoSystemRegistry>
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
