using Anabasis.Common;
using Anabasis.Common.Actor;
using Anabasis.Common.Events;
using Anabasis.Common.Mediator;
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

      Mediator.Emit(new DocumentCreated(exportDocumentRequest.CorrelationID, exportDocumentRequest.StreamId, exportDocumentRequest.TopicId, anabasisDocument));

      return Task.CompletedTask;

    }
  }
}
