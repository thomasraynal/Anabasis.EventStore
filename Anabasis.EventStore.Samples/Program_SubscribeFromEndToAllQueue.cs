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
                                       .WithSubscribeFromEndToAllStream()
                                       .Build();

            Do.Run(eventCountActor);

        }
    }
}
