using Anabasis.Common.Infrastructure;
using System;

namespace Anabasis.Common.Events
{
  public class DocumentCreated : BaseEvent
  {
    public DocumentCreated(Guid correlationId, string streamId, string topicId, AnabasisDocument anabasisDocument) : base(correlationId, streamId, topicId)
    {
      Document = anabasisDocument;
    }

    public AnabasisDocument Document { get; set; }

    public override string Log()
    {
      return $"Document exported - {Document.Id}";
    }
  }
}
