using Anabasis.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public class StartExportCommand : BaseCommand
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
