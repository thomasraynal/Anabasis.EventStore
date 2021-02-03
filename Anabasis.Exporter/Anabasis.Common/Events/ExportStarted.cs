using Anabasis.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public class ExportStarted : BaseEvent
  {
    public ExportStarted(Guid correlationId, string[] documentsIds, string streamId, string topicId) : base(correlationId, streamId, topicId)
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
