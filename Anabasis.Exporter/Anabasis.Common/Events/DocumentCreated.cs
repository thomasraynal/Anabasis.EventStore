using Anabasis.Common.Infrastructure;
using System;

namespace Anabasis.Common.Events
{
  public class DocumentCreated : BaseEvent
  {
    public DocumentCreated(Guid correlationId, string streamId, string topicId, string documentId, Uri documentUrl) : base(correlationId, streamId, topicId)
    {
      DocumentUri = documentUrl;
      DocumentId = documentId;
    }

    public Uri DocumentUri { get; set; }
    public string DocumentId { get; set; }

    public override string Log()
    {
      return $"Document exported @ {DocumentId}";
    }
  }
}
