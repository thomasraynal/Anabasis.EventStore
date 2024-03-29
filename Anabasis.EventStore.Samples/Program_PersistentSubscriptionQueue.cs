﻿using Anabasis.Common;
using Anabasis.EventStore.Standalone.Embedded;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.Core;
using System;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Samples
{
    public class Program_PersistentSubscriptionStream
    {

        private static async Task CreateSubscriptionGroups(ClusterVNode clusterVNode)
        {

            var connectionSettings = PersistentSubscriptionSettings.Create().StartFromCurrent().PreferRoundRobin().Build();
            var connection = EmbeddedEventStoreConnection.Create(clusterVNode);

            await connection.CreatePersistentSubscriptionAsync(
                 StaticData.PersistentStreamOne,
                 StaticData.GroupIdOne,
                 connectionSettings,
                 StaticData.EventStoreUserCredentials);

            await connection.CreatePersistentSubscriptionAsync(
                 StaticData.PersistentStreamOne,
                 StaticData.GroupIdTwo,
                 connectionSettings,
                 StaticData.EventStoreUserCredentials);
        }

        public static void Run()
        {

            var clusterVNode = EmbeddedVNodeBuilder
              .AsSingleNode()
              .RunInMemory()
              .StartStandardProjections()
              .WithWorkerThreads(1)
              .Build();

            clusterVNode.StartAsync(true).Wait();

            CreateSubscriptionGroups(clusterVNode).Wait();

            var eventCountActorOne = EventStoreEmbeddedStatelessActorBuilder<EventCountStatelessActor, DemoSystemRegistry>
                                       .Create(clusterVNode, Do.GetConnectionSettings())
                                        .WithBus<IEventStoreBus>((actor, bus) =>
                                        {
                                            actor.SubscribeToPersistentSubscriptionStream(StaticData.PersistentStreamOne, StaticData.GroupIdOne);
                                        })
                                       .Build();

            var eventCountActorTwo = EventStoreEmbeddedStatelessActorBuilder<EventCountStatelessActor, DemoSystemRegistry>
                                       .Create(clusterVNode, Do.GetConnectionSettings())
                                        .WithBus<IEventStoreBus>((actor, bus) =>
                                        {
                                            actor.SubscribeToPersistentSubscriptionStream(StaticData.PersistentStreamOne, StaticData.GroupIdOne);
                                        })
                                       .Build();

            var position = 0;

            while (true)
            {
                if (position == 0)
                {
                    Console.WriteLine("Press enter to generate random event...");
                }

                Console.ReadLine();

                Console.WriteLine($"Generating {nameof(EventCountOne)} for stream {StaticData.PersistentStreamOne}");
                eventCountActorOne.EmitEventStore(new EventCountOne(position++, StaticData.PersistentStreamOne, Guid.NewGuid())).Wait();
            }

         

        }
    }
}
