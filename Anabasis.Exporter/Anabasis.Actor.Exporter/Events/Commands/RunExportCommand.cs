using Anabasis.Actor;
using System;

namespace Anabasis.Common.Events
{
  public class RunExportCommand : BaseAnabasisExporterCommand
  {
    public RunExportCommand(Guid correlationId, string streamId) : base(correlationId, streamId, $"{streamId}-{DateTime.UtcNow:yyyyMMddHHmmss}")
    {
    }

    public override string Log()
    {
      return $"{nameof(RunExportCommand)} - {StreamId}";
    }
  }
}
