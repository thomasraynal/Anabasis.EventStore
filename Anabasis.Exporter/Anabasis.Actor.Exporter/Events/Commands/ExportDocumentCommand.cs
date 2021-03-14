using Anabasis.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public class ExportDocumentCommand : BaseAnabasisExporterCommand
  {
    public ExportDocumentCommand(Guid correlationId,
      string streamId,
      string documentId,
      string url= null) : base(correlationId, streamId)
    {
      DocumentId = documentId;
      DocumentUrl = url;
    }

    public string DocumentId { get; set; }
    public string DocumentUrl { get; set; }

    public override string Log()
    {
      return $"{nameof(ExportDocumentCommand)} - {DocumentId} ";
    }
  }
}
