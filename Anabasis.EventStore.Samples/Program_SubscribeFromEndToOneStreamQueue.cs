using Anabasis.Common;
using Anabasis.EventStore.Standalone;

namespace Anabasis.EventStore.Samples
{
    public class Program_SubscribeFromEndToOneStreamStream
    {

        public static void Run()
        {

      
            var eventCountActor = EventStoreStatelessActorBuilder<EventCountStatelessActor, DemoSystemRegistry>
                                       .Create(StaticData.EventStoreUrl, Do.GetConnectionSettings(), ActorConfiguration.Default)
                                        .WithBus<IEventStoreBus>((actor, bus) =>
                                        {
                                            actor.SubscribeToOneStream(StaticData.EntityOne);
                                        })
                                       .Build();

            Do.Run(eventCountActor);

        }
    }
}
