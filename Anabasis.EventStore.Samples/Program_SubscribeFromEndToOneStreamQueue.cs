using Anabasis.Common;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Standalone;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Samples
{
    public class Program_SubscribeFromEndToOneStreamStream
    {

        public static void Run()
        {

      
            var eventCountActor = StatelessActorBuilder<EventCountStatelessActor, DemoSystemRegistry>
                                       .Create(StaticData.EventStoreUrl, Do.GetConnectionSettings(), ActorConfiguration.Default)
                                       .WithSubscribeFromEndToOneStreamStream(streamId: StaticData.EntityOne)
                                       .Build();

            Do.Run(eventCountActor);

        }
    }
}
