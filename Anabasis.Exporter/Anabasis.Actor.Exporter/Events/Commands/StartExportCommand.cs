using Anabasis.Actor;
using System;

namespace Anabasis.Common.Events
{
  public class StartExportCommand : BaseAnabasisExporterCommand
  {
    public StartExportCommand(Guid correlationId, string streamId) : base(correlationId, streamId, $"{streamId}-{DateTime.UtcNow:yyyyMMddHHmmss}")
    {
    }

    public override string Log()
    {
      return "Starting export";
    }
  }
}
