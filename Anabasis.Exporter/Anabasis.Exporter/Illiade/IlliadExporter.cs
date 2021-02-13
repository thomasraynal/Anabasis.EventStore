using Anabasis.Common;
using Anabasis.Common.Actor;
using Anabasis.Common.Events;
using Anabasis.Common.Mediator;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Anabasis.Exporter.Illiade
{
  [InMemoryInstance(5)]
  public class IlliadExporter : BaseActor
  {

    public IlliadExporter(IMediator simpleMediator) : base(simpleMediator)
    {
    }

    public override string StreamId => StreamIds.Illiad;

    public Task Handle(ExportDocument exportDocumentRequest)
    {

      var documentBuilder = new IlliadDocumentBuilder(exportDocumentRequest.DocumentTitle,
        exportDocumentRequest.DocumentId,
        exportDocumentRequest.DocumentUrl);

      var anabasisDocument = documentBuilder.BuildDocument();

      var path = Path.GetFullPath($"{anabasisDocument.Id}");

      File.WriteAllText(path, JsonConvert.SerializeObject(anabasisDocument));

      Mediator.Emit(new DocumentCreated(exportDocumentRequest.CorrelationID, exportDocumentRequest.StreamId, exportDocumentRequest.TopicId, anabasisDocument.Id, new Uri(path)));

      return Task.CompletedTask;

    }
  }
}
