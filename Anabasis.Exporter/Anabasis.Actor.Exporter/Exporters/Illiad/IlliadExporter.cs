using Anabasis.Actor;
using Anabasis.Common.Events;
using Anabasis.EventStore;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Anabasis.Exporter.Illiad
{

  public class IlliadExporter : BaseActor
  {

    public IlliadExporter(IEventStoreRepository eventStoreRepository) : base(eventStoreRepository)
    {
    }

    public async Task Handle(ExportDocumentCommand exportDocumentRequest)
    {

      var documentBuilder = new IlliadDocumentBuilder(
        exportDocumentRequest.DocumentId,
        exportDocumentRequest.DocumentUrl);

      var anabasisDocument = documentBuilder.Build();

      var path = Path.GetFullPath($"{documentBuilder.DocumentId}");

      File.WriteAllText(path, anabasisDocument.ToJson());

      await Emit(new DocumentCreated(exportDocumentRequest.CorrelationID, exportDocumentRequest.StreamId, documentBuilder.DocumentId, new Uri(path)));


    }
  }
}
