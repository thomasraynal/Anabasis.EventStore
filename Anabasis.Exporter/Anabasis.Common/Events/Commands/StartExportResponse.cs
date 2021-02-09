using Anabasis.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events.Commands
{
  public class StartExportResponse : BaseCommandResponse
  {
    public StartExportResponse(Guid correlationId, string streamId, string topicId) : base(correlationId, streamId, topicId)
    {
    }
    public override string Log()
    {
      return "Export Ended";
    }
  }
}
