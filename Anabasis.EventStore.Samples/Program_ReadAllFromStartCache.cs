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


    public class Program_ReadAllFromStartCache
    {

        public static void Run()
        {
            
            var eventTypeProvider = new DefaultEventTypeProvider<EventCountAggregate>(() => new[] { typeof(EventCountOne), typeof(EventCountTwo) }); ;

            var eventCountActor = EventStoreStatefulActorBuilder<EventCountStatefulActor, EventCountAggregate, DemoSystemRegistry>
                                       .Create(StaticData.EventStoreUrl, Do.GetConnectionSettings(), ActorConfiguration.Default)
                                       .WithReadAllFromStartCache(
                                            eventTypeProvider: eventTypeProvider,
                                            getCatchupEventStoreCacheConfigurationBuilder: builder => builder.KeepAppliedEventsOnAggregate = true)
                                       .Build();

            Do.Run(eventCountActor);

        }
    }
}
