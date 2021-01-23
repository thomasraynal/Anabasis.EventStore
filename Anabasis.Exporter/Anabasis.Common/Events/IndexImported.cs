using Anabasis.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public class IndexImported : BaseEvent
  {
    public IndexImported(DocumentIndex documentIndex, Guid correlationId) : base(correlationId)
    {
      Index = documentIndex;
    }

    public DocumentIndex Index { get; set; }

    public override string Log()
    {
      return $"Index imported - {Index.Title}";

    }

  }
}
