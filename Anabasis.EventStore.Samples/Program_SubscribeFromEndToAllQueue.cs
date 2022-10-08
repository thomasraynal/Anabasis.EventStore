using Anabasis.Common;
using Anabasis.EventStore.Standalone;
using EventStore.ClientAPI;

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
                                            actor.SubscribeToAllStreams(Position.End);
                                        })
                                       .Build();

            Do.Run(eventCountActor);

        }
    }
}
