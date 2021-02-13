using Anabasis.Importer;
using EventStore.Core;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using System;
using System.Text;
using System.Threading.Tasks;
using EventStore.Core.Data;
using NUnit.Framework;

namespace Anabasis.Exporter.Tests
{

  public class TestService
  {

    [Test]
    public async Task ShouldTestActor()
    {

      //var node = EmbeddedVNodeBuilder
      //.AsSingleNode()
      //.RunInMemory()
      //.OnDefaultEndpoints()
      //.Build();

      //bool isNodeMaster = false;
      //node.NodeStatusChanged += (sender, args) => {
      //  isNodeMaster = args.NewVNodeState == VNodeState.Manager;
      //};
      //node.Start();
      
      //var connection = EmbeddedEventStoreConnection.Create(node);

      //await connection.ConnectAsync();

      //const string streamName = "newstream";
      //const string eventType = "event-type";
      //const string data = "{ \"a\":\"2\"}";
      //const string metadata = "{}";

      //var eventPayload = new EventData(
      //    eventId: Guid.NewGuid(),
      //    type: eventType,
      //    isJson: true,
      //    data: Encoding.UTF8.GetBytes(data),
      //    metadata: Encoding.UTF8.GetBytes(metadata)
      //);
      //var result = await connection.AppendToStreamAsync(streamName, EventStore.ClientAPI.ExpectedVersion.NoStream, eventPayload);


      //var readEvents = await connection.ReadStreamEventsForwardAsync(streamName, 0, 10, true);

      //foreach (var evt in readEvents.Events)
      //{
      //  Console.WriteLine(Encoding.UTF8.GetString(evt.Event.Data));
      //}

    }

    [Test]
    public async Task ShouldExportFolder()
    {
      //Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

      //var fileSystemDocumentRepositoryConfiguration = new FileSystemDocumentRepositoryConfiguration()
      //{
      //  ClientId = "699173273524-aalbhs95og7ci38ink060v8bj166mej3.apps.googleusercontent.com",
      //  ClientSecret = "_9La2dPSNsZtRgo-0fUG00kV",
      //  DriveRootFolder = "1e-fnRCTrPxpbo6Aq7-xiw3sQraFvQ-XM",
      //  LocalDocumentFolder = @"E:\dev\anabasis\src\assets",
      //  RefreshToken = "1//03dpBmmfoO2X3CgYIARAAGAMSNwF-L9Ir9qcyc48kXirb_mr2yyPt8vnA4sJvSATu8EaScrKjb5-nyzw2uP69sP_EftPdrVy6YDE"
      //};

      //var fileSystemDocumentRepository = new FileSystemDocumentRepository(fileSystemDocumentRepositoryConfiguration);

      //var documentService = new AnabasisExporter<FileSystemDocumentRepositoryConfiguration>(fileSystemDocumentRepositoryConfiguration, fileSystemDocumentRepository);

      //await documentService.GetExportedDocuments();


    }
  }
}
