using Anabasis.Common;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Standalone;
using Anabasis.EventStore.Standalone.Embedded;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;

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

            var allStreamsCatchupCacheConfiguration = new AllStreamsCatchupCacheConfiguration(Position.End)
            {
                KeepAppliedEventsOnAggregate = true
            };

            var eventCountActor = EventStoreEmbeddedStatefulActorBuilder<EventCountStatefulActor, AllStreamsCatchupCacheConfiguration, EventCountAggregate, DemoSystemRegistry>
                                       .Create(clusterVNode, Do.GetConnectionSettings(), ActorConfiguration.Default, allStreamsCatchupCacheConfiguration, eventTypeProvider)
                                       .Build();

            Do.Run(eventCountActor);

        }
    }
}
