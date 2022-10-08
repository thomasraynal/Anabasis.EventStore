using Anabasis.Common;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Standalone;
using Anabasis.EventStore.Standalone.Embedded;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Anabasis.EventStore.Samples
{


    public class Program_ReadAllFromStartCache
    {

        public static void Run()
        {
            var clusterVNode = EmbeddedVNodeBuilder
              .AsSingleNode()
              .RunInMemory()
              .WithWorkerThreads(1)
              .Build();

            clusterVNode.StartAsync(true).Wait();


            Log.Logger = new LoggerConfiguration()
              .MinimumLevel.Information()
              .MinimumLevel.Override("Microsoft", LogEventLevel.Debug)
              .WriteTo.Console()
              .CreateLogger();

            var loggerFactory = new LoggerFactory().AddSerilog(Log.Logger);

            var eventTypeProvider = new DefaultEventTypeProvider<EventCountAggregate>(() => new[] { typeof(EventCountOne), typeof(EventCountTwo) }); ;


            var allStreamsCatchupCacheConfiguration = new AllStreamsCatchupCacheConfiguration(Position.Start)
            {
                KeepAppliedEventsOnAggregate = true
            };

            var eventCountActor = EventStoreEmbeddedStatefulActorBuilder<EventCountStatefulActor, AllStreamsCatchupCacheConfiguration, EventCountAggregate, DemoSystemRegistry >
                                       .Create(clusterVNode, Do.GetConnectionSettings(), ActorConfiguration.Default, allStreamsCatchupCacheConfiguration, eventTypeProvider, loggerFactory)
                                       .Build();

            Do.Run(eventCountActor);

        }
    }
}
