using Anabasis.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public class IndexCreated : BaseAnabasisExporterEvent
  {
    public IndexCreated(string documentId, Uri documentIndexUri, Guid correlationId, string streamId) : base(correlationId, streamId)
    {
      AnabasisDocumentIndexUri = documentIndexUri;
      DocumentId = documentId;
    }

    public Uri AnabasisDocumentIndexUri { get; set; }
    public string DocumentId { get; set; }

    public override string Log()
    {
      return $"{nameof(IndexCreated)} - {DocumentId} ";
    }
  }
}
