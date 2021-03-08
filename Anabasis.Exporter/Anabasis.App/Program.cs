using Anabasis.Actor;
using Anabasis.Actor.Actor;
using Anabasis.Actor.Exporter.Exporters.Bobby;
using Anabasis.Common;
using Anabasis.Common.Events;
using Anabasis.Common.Events.Commands;
using Anabasis.Common.Infrastructure;
using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure;
using Anabasis.EventStore.Infrastructure.Queue;
using Anabasis.EventStore.Infrastructure.Queue.PersistentQueue;
using Anabasis.EventStore.Infrastructure.Queue.VolatileQueue;
using Anabasis.Exporter;
using Anabasis.Exporter.Bobby;
using Anabasis.Importer;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using EventStore.Core;
using Lamar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.App
{
  class Program
  {
    private static UserCredentials _userCredentials;
    private static ConnectionSettings _connectionSettings;
    private static ClusterVNode _clusterVNode;

    public static async Task SetupEventStore()
    {

      _userCredentials = new UserCredentials("admin", "changeit");
      _connectionSettings = ConnectionSettings.Create().UseDebugLogger().KeepRetrying().Build();

      _clusterVNode = EmbeddedVNodeBuilder
        .AsSingleNode()
        .RunInMemory()
        .RunProjections(ProjectionType.All)
        .StartStandardProjections()
        .WithWorkerThreads(1)
        .Build();

      await _clusterVNode.StartAsync(true);

      await CreateSubscriptionGroups();

    }

    private static async Task CreateSubscriptionGroups()
    {
      var connectionSettings = PersistentSubscriptionSettings.Create().StartFromCurrent().Build();
      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode);

      foreach (var streamId in StreamIds.AllStreams)
      {

        for (var i = 0; i < StreamIds.PersistentSubscriptionGroupCount; i++)
        {
          await connection.CreatePersistentSubscriptionAsync(streamId, $"{streamId}_{i}", connectionSettings, _userCredentials);
        }

      }

    }



    static void Main(string[] args)
    {

      SetupEventStore().Wait();

      var bobbyDispatcher = ActorBuilder<BobbyDispatcher, FileSystemRegistry>.Create(_clusterVNode, _userCredentials, _connectionSettings)
                                         .WithSubscribeToAllQueue()
                                         .Build();

      var bobbyExporterOne = ActorBuilder<BobbyExporter, FileSystemRegistry>.Create(_clusterVNode, _userCredentials, _connectionSettings)
                                                 .WithPersistentSubscriptionQueue(StreamIds.Bobby, $"{StreamIds.Bobby}_0")
                                               .Build();

      var bobbyExporterTwo = ActorBuilder<BobbyExporter, FileSystemRegistry>.Create(_clusterVNode, _userCredentials, _connectionSettings)
                                                 .WithPersistentSubscriptionQueue(StreamIds.Bobby, $"{StreamIds.Bobby}_1")
                                               .Build();

      var bobbyIndexerOne = ActorBuilder<Indexer, FileSystemRegistry>.Create(_clusterVNode, _userCredentials, _connectionSettings)
                                               .WithSubscribeToAllQueue()
                                               .Build();

      var bobbyImporter = ActorBuilder<FileSystemDocumentRepository, FileSystemRegistry>.Create(_clusterVNode, _userCredentials, _connectionSettings)
                                               .WithSubscribeToAllQueue()
                                               .Build();

      var allEvents = typeof(StartExportCommand).Assembly.GetTypes()
                                    .Where(type => type.GetInterfaces().Any(@interface => @interface == typeof(IEvent))).ToArray();

      var allEventsProvider = new DefaultEventTypeProvider(() => allEvents);

      var logger = ActorBuilder<Logger, FileSystemRegistry>.Create(_clusterVNode, _userCredentials, _connectionSettings, eventTypeProvider: allEventsProvider)
                                         .WithSubscribeToAllQueue(allEventsProvider)
                                         .Build();

      var startExport = new StartExportCommand(Guid.NewGuid(), StreamIds.Bobby);


      Task.Run( async() =>
      {

        var result = await bobbyDispatcher.Send<StartExportCommandResponse>(startExport);

      });




      //var mediator = World.Create<FileSystemRegistry>();

      //mediator.Emit(new StartExportCommand(Guid.NewGuid(), StreamIds.Illiad));





      Console.Read();

    }
  }
}
