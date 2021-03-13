using Anabasis.Common.Events;
using System;

namespace Anabasis.Actor.Exporter.Exporters.Bobby
{
  public class DocumentBuildRequested : BaseAnabasisExporterEvent
  {
    public DocumentBuildRequested(Guid correlationId,
   string streamId,
   string documentId,
   (string documentUrl, string documentHeading)[] documentsBatch) : base(correlationId, streamId)
    {
      DocumentId = documentId;
      DocumentBuilderBatch = documentsBatch;
    }

    public (string documentUrl, string documentHeading)[] DocumentBuilderBatch { get; set; }

    public string DocumentId { get; set; }
    public override string Log()
    {
      return $"{nameof(DocumentBuildRequested)} - {DocumentId}";
    }
  }
}
