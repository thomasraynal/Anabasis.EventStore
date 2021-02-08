using Anabasis.Common.Infrastructure;
using System;

namespace Anabasis.Common.Events
{
  public class DocumentImported : BaseEvent
  {
    public DocumentImported(AnabasisDocument anabasisDocument, Guid correlationId, string streamId, string topicId) : base(correlationId, streamId, topicId)
    {
      Document = anabasisDocument;
    }

    public AnabasisDocument Document { get; set; }

    public override string Log()
    {
      return $"Document imported - {Document.Id}";
    }
  }
}
