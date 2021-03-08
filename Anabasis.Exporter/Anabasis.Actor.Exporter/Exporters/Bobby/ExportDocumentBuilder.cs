using Anabasis.Common.Events;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Actor.Exporter.Exporters.Bobby
{
  public class ExportDocumentBuilder : BaseAnabasisExporterEvent
  {
    public ExportDocumentBuilder(Guid correlationId,
   string streamId,
   string topicId,
   string documentId,
   (string documentUrl, string documentHeading)[] documentsBatch) : base(correlationId, streamId, topicId)
    {
      DocumentId = documentId;
      DocumentBuilderBatch = documentsBatch;
    }

    public (string documentUrl, string documentHeading)[] DocumentBuilderBatch { get; set; }

    public string DocumentId { get; set; }
    public override string Log()
    {
      return $"Document Export Requested - {DocumentId}";
    }
  }
}
