using Anabasis.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events.Commands
{
  public class RunExportCommandResponse : BaseAnabasisExporterCommandResponse
  {
    public RunExportCommandResponse(Guid commandId, Guid correlationId, string streamId) : base(commandId, correlationId, streamId)
    {
    }
    public override string Log()
    {
      return "Export Ended";
    }
  }
}
