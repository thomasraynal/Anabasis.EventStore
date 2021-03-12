using Anabasis.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events.Commands
{
  public class RunExportCommandResponse : BaseAnabasisExporterCommandResponse
  {
    public RunExportCommandResponse(Guid commandId, Guid correlationId, string streamId, string topicId) : base(commandId, correlationId, streamId, topicId)
    {
    }
    public override string Log()
    {
      return "Export Ended";
    }
  }
}
