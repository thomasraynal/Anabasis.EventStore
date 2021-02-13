using Anabasis.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public class IndexCreated : BaseEvent
  {
    public IndexCreated(string documentId, Uri documentIndexUri, Guid correlationId, string streamId, string topicId) : base(correlationId, streamId, topicId)
    {
      AnabasisDocumentIndexUri = documentIndexUri;
      DocumentId = documentId;
    }

    public Uri AnabasisDocumentIndexUri { get; set; }
    public string DocumentId { get; set; }

    public override string Log()
    {
      return $"Index exported - {DocumentId}";
    }
  }
}
