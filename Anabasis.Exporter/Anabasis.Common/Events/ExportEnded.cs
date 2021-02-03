using Anabasis.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public class ExportEnded : BaseEvent, ICommand
  {
    public ExportEnded(Guid correlationId, string streamId, string topicId) : base(correlationId, streamId, topicId)
    {
    }

    public override string Log()
    {
      return "Export has ended";
    }
  }
}
