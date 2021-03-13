using System;

namespace Anabasis.Common.Events
{
  public class DocumentCreationFailed : BaseAnabasisExporterEvent
  {
    public DocumentCreationFailed(Guid correlationId, string streamId,  string documentId) : base(correlationId, streamId)
    {
      DocumentId = documentId;
    }

    public string DocumentId { get; set; }

    public override string Log()
    {
      return $"{nameof(DocumentCreationFailed)} - {DocumentId}";
    }
  }
}
