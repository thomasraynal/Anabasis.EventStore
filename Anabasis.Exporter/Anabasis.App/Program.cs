using Anabasis.Actor.Exporter;
using Anabasis.Actor.Exporter.Exporters.Bobby;
using Anabasis.Common;
using Anabasis.Common.Events;
using Anabasis.Common.Events.Commands;
using Anabasis.Exporter;
using Anabasis.Exporter.Bobby;
using Anabasis.Exporter.GoogleDoc;
using Anabasis.Exporter.Illiad;
using Anabasis.Importer;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.App
{
  class Program
  {

    static void Main(string[] args)
    {

      Task.Run(async () =>
     {

       var userCredentials = new UserCredentials("admin", "changeit");
       var connectionSettings = ConnectionSettings.Create().UseDebugLogger().KeepRetrying().Build();

       //var actors = await World.Create<FileSystemRegistry, BobbyDispatcher, BobbyExporter, DummyIndexer, BobbyFileSystemDocumentRepository>(StreamIds.Bobby, userCredentials, connectionSettings, 5, 5);

       var actors = await World.Create<FileSystemRegistry, IlliadDispatcher, IlliadExporter, DummyIndexer, IlliadFileSystemDocumentRepository>(StreamIds.Illiad, userCredentials, connectionSettings, 5,5);

      // var actors = await World.Create<FileSystemRegistry, GoogleDocDispatcher, GoogleDocExporter, GoogleDocIndexer, GoogleDocFileSystemDocumentRepository>(StreamIds.GoogleDoc, userCredentials, connectionSettings, 5, 5);

       var mediator = actors.First();

       var result = await mediator.Send<RunExportCommandResponse>(new RunExportCommand(Guid.NewGuid(), StreamIds.Illiad));


     });


      Console.Read();

    }
  }
}
