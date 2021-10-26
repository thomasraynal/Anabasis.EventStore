using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Standalone;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Samples
{
    public class Program_SubscribeFromEndToOneStreamQueue
    {

        public static void Run()
        {

      
            var eventCountActor = StatelessActorBuilder<EventCountStatelessActor, DemoSystemRegistry>
                                       .Create(StaticData.EventStoreUrl, Do.GetConnectionSettings())
                                       .WithSubscribeFromEndToOneStreamQueue(streamId: StaticData.EntityOne)
                                       .Build();

            Do.Run(eventCountActor);

        }
    }
}
