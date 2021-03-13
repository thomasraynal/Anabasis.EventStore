using System;

namespace Anabasis.Common.Events
{
  public class DocumentCreated : BaseAnabasisExporterEvent
  {
    public DocumentCreated(Guid correlationId, string streamId, string documentId, Uri documentUrl) : base(correlationId, streamId)
    {
      DocumentUri = documentUrl;
      DocumentId = documentId;
    }

    public Uri DocumentUri { get; set; }
    public string DocumentId { get; set; }

    public override string Log()
    {
      return $"{nameof(DocumentCreated)} - {DocumentId}";
    }
  }
}
