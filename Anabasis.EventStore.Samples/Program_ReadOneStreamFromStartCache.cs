using Anabasis.Common;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Standalone;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Lamar;
using System;

namespace Anabasis.EventStore.Samples
{
    public static class Program_ReadOneStreamFromStartCache
    {

        public static void Run()
        {
            var eventTypeProvider = new DefaultEventTypeProvider<EventCountAggregate>(() => new[] { typeof(EventCountOne), typeof(EventCountTwo) }); ;

            var eventCountActor = StatefulActorBuilder<EventCountStatefulActor, EventCountAggregate, DemoSystemRegistry>
                                       .Create(StaticData.EventStoreUrl, Do.GetConnectionSettings(), ActorConfiguration.Default)
                                       .WithReadOneStreamFromStartCache(StaticData.EntityOne,
                                            eventTypeProvider: eventTypeProvider,
                                            getMultipleStreamsCatchupCacheConfiguration: builder => builder.KeepAppliedEventsOnAggregate = true)
                                       .Build();


            Do.Run(eventCountActor);
        }

    }
}
