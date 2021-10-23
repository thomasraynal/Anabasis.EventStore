using Anabasis.EventStore.Actor;
using Anabasis.EventStore.EventProvider;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Lamar;
using System;

namespace Anabasis.EventStore.Samples
{


    public class Program_ReadAllFromStartCache
    {

        public static void Run()
        {
            
            var eventTypeProvider = new DefaultEventTypeProvider<string, EventCountAggregate>(() => new[] { typeof(EventCountOne), typeof(EventCountTwo) }); ;

            var eventCountActor = StatefulActorBuilder<EventCountStatefulActor, string, EventCountAggregate, DemoSystemRegistry>
                                       .Create(StaticData.EventStoreUrl, Do.GetConnectionSettings())
                                       .WithReadAllFromStartCache(
                                            eventTypeProvider: eventTypeProvider,
                                            getCatchupEventStoreCacheConfigurationBuilder: builder => builder.KeepAppliedEventsOnAggregate = true)
                                       .Build();

            Do.Run(eventCountActor);

        }
    }
}
