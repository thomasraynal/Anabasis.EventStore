using Anabasis.Common;
using Anabasis.EventStore.Standalone;

namespace Anabasis.EventStore.Samples
{


    public class Program_ReadManyStreamFromStartCache
    {

        public  static void Run()
        {

            
            var eventTypeProvider = new DefaultEventTypeProvider<EventCountAggregate>(() => new[] { typeof(EventCountOne), typeof(EventCountTwo) }); ;

            var eventCountActor = EventStoreStatefulActorBuilder<EventCountStatefulActor, EventCountAggregate, DemoSystemRegistry>
                                       .Create(StaticData.EventStoreUrl, Do.GetConnectionSettings(), ActorConfiguration.Default)
                                       .WithReadManyStreamsFromStartCache(
                                            new[] { StaticData.EntityOne, StaticData.EntityTwo, StaticData.EntityThree },
                                            eventTypeProvider: eventTypeProvider,
                                            getMultipleStreamsCatchupCacheConfiguration: builder => builder.KeepAppliedEventsOnAggregate = true)
                                       .Build();


            Do.Run(eventCountActor);

        }
    }
}
