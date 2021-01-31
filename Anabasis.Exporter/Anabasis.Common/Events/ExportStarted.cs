using Anabasis.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public class ExportStarted : BaseEvent
  {
    public ExportStarted(Guid correlationId, string[] documentsIds) : base(correlationId)
    {
      DocumentsIds = documentsIds;
    }

    public string[] DocumentsIds { get; }

    public override string Log()
    {
      return $"Export started ({DocumentsIds.Length} document(s))";
    }
  }
}
