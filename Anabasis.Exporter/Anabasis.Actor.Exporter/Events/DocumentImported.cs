using Anabasis.Common.Infrastructure;
using System;

namespace Anabasis.Common.Events
{
  public class DocumentImported : BaseAnabasisExporterEvent
  {
    public DocumentImported(string documentId, Guid correlationId, string streamId, string topicId) : base(correlationId, streamId, topicId)
    {
      DocumentId = documentId;
    }

    public string DocumentId { get; set; }

    public override string Log()
    {
      return $"{nameof(DocumentImported)} - {DocumentId}";
    }
  }
}
