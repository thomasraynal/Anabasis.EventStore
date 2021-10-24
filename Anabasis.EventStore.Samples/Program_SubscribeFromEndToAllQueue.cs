using Anabasis.EventStore.Actor;
using Anabasis.EventStore.EventProvider;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using Lamar;
using System;

namespace Anabasis.EventStore.Samples
{

    public class Program_SubscribeFromEndToAllQueue
    {

        public static void Run()
        {

            var eventCountActor = StatelessActorBuilder<EventCountStatelessActor, DemoSystemRegistry>
                                       .Create(StaticData.EventStoreUrl, Do.GetConnectionSettings())
                                       .WithSubscribeFromEndToAllQueue()
                                       .Build();

            Do.Run(eventCountActor);

        }
    }
}
