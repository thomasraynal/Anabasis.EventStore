using Anabasis.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public class StartExportRequest : BaseCommand
  {
    public StartExportRequest(Guid correlationId, string streamId) : base(Guid.NewGuid(), correlationId, streamId, $"{streamId}-{DateTime.UtcNow:yyyyMMddHHmmss}")
    {
    }

    public override string Log()
    {
      return "Starting export";
    }
  }
}
