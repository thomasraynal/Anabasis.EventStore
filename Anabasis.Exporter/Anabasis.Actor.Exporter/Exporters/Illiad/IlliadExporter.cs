using Anabasis.Actor;
using Anabasis.Common.Events;
using Anabasis.EventStore;
using Newtonsoft.Json;
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

    public async Task Handle(ExportDocument exportDocumentRequest)
    {

      var documentBuilder = new IlliadDocumentBuilder(exportDocumentRequest.DocumentTitle,
        exportDocumentRequest.DocumentId,
        exportDocumentRequest.DocumentUrl);

      var anabasisDocument = documentBuilder.BuildDocument();

      var path = Path.GetFullPath($"{anabasisDocument.Id}");

      File.WriteAllText(path, JsonConvert.SerializeObject(anabasisDocument));

      await Emit(new DocumentCreated(exportDocumentRequest.CorrelationID, exportDocumentRequest.StreamId, exportDocumentRequest.TopicId, anabasisDocument.Id, new Uri(path)));


    }
  }
}
