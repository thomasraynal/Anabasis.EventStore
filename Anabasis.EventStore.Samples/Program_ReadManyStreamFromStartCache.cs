using Anabasis.Common;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Standalone;

namespace Anabasis.EventStore.Samples
{


    public class Program_ReadManyStreamFromStartCache
    {

        public  static void Run()
        {

            
            var eventTypeProvider = new DefaultEventTypeProvider<EventCountAggregate>(() => new[] { typeof(EventCountOne), typeof(EventCountTwo) }); ;

            var multipleStreamsCatchupCacheConfiguration = new MultipleStreamsCatchupCacheConfiguration(StaticData.EntityOne, StaticData.EntityTwo, StaticData.EntityThree)
            {
                KeepAppliedEventsOnAggregate = true
            };

            var eventCountActor = EventStoreStatefulActorBuilder<EventCountStatefulActor2, MultipleStreamsCatchupCacheConfiguration, EventCountAggregate, DemoSystemRegistry>
                                       .Create(StaticData.EventStoreUrl, 
                                            Do.GetConnectionSettings(),
                                            multipleStreamsCatchupCacheConfiguration,
                                            ActorConfiguration.Default, 
                                            eventTypeProvider)
                                       .Build();


            Do.Run(eventCountActor);

        }
    }
}
