using Anabasis.Common.Infrastructure;
using System;

namespace Anabasis.Common.Events
{
  public class DocumentExported : BaseEvent
  {
    public DocumentExported(Guid correlationId, AnabasisDocument anabasisDocument) : base(correlationId)
    {
      Document = anabasisDocument;
    }

    public AnabasisDocument Document { get; set; }

    public override string Log()
    {
      return $"Document exported - {Document.Title}";
    }
  }
}
