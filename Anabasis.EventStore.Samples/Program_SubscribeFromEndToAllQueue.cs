using Anabasis.Common;
using Anabasis.EventStore.Standalone;

namespace Anabasis.EventStore.Samples
{

    public class Program_SubscribeFromEndToAllStream
    {

        public static void Run()
        {

            var eventCountActor = EventStoreStatelessActorBuilder<EventCountStatelessActor, DemoSystemRegistry>
                                       .Create(StaticData.EventStoreUrl, Do.GetConnectionSettings(), ActorConfiguration.Default)
                                        .WithBus<IEventStoreBus>((actor, bus) =>
                                        {
                                            actor.SubscribeFromEndToAllStreams();
                                        })
                                       .Build();

            Do.Run(eventCountActor);

        }
    }
}
