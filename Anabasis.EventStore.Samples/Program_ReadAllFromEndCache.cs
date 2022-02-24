﻿using Anabasis.Common;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Standalone;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using Lamar;
using System;

namespace Anabasis.EventStore.Samples
{

    public class Program_ReadAllFromEndCache
    {

        public static void Run()
        {


            var clusterVNode = EmbeddedVNodeBuilder
              .AsSingleNode()
              .RunInMemory()
              .WithWorkerThreads(1)
              .Build();

            clusterVNode.StartAsync(true).Wait();

            var eventTypeProvider = new DefaultEventTypeProvider<EventCountAggregate>(() => new[] { typeof(EventCountOne), typeof(EventCountTwo) }); ;

            var eventCountActor = EventStoreStatefulActorBuilder<EventCountStatefulActor, EventCountAggregate, DemoSystemRegistry>
                                       .Create(clusterVNode, Do.GetConnectionSettings(), ActorConfiguration.Default)
                                       .WithReadAllFromEndCache(
                                            eventTypeProvider: eventTypeProvider,
                                            getSubscribeFromEndCacheConfiguration: builder => builder.KeepAppliedEventsOnAggregate = true)
                                       .Build();

            Do.Run(eventCountActor);

        }
    }
}
