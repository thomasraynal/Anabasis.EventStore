using Anabasis.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public class DocumentCreationFailed : BaseEvent
  {
    public DocumentCreationFailed(Guid correlationId, string streamId, string topicId, string documentId) : base(correlationId, streamId, topicId)
    {
      DocumentId = documentId;
    }

    public string DocumentId { get; set; }

    public override string Log()
    {
      return $"Failed to create - {DocumentId}";
    }
  }
}
