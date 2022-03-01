using Anabasis.Common;
using Anabasis.EventStore.Standalone;
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

            var eventCountActor = EventStoreStatefulActorBuilder<EventCountStatefulActor, EventCountAggregate, DemoSystemRegistry>
                                       .Create(clusterVNode, Do.GetConnectionSettings(), ActorConfiguration.Default, loggerFactory)
                                       .WithReadAllFromStartCache(
                                            eventTypeProvider: eventTypeProvider,
                                            getCatchupEventStoreCacheConfigurationBuilder: builder => builder.KeepAppliedEventsOnAggregate = true)
                                       .Build();

            Do.Run(eventCountActor);

        }
    }
}
