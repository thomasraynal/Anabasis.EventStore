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

    public class Program_SubscribeFromEndToAllStream
    {

        public static void Run()
        {

            var eventCountActor = StatelessActorBuilder<EventCountStatelessActor, DemoSystemRegistry>
                                       .Create(StaticData.EventStoreUrl, Do.GetConnectionSettings(), ActorConfiguration.Default)
                                       .WithSubscribeFromEndToAllStream()
                                       .Build();

            Do.Run(eventCountActor);

        }
    }
}
