using Anabasis.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public class IndexImported : BaseEvent
  {
    public IndexImported(string documentId, Guid correlationId, string streamId, string topicId) : base(correlationId, streamId, topicId)
    {
      DocumentId = documentId;
    }

    public string DocumentId { get; set; }

    public override string Log()
    {
      return $"Index imported - {DocumentId}";
    }

  }
}
