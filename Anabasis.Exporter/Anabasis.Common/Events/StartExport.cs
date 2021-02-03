using Anabasis.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public class StartExport : BaseEvent, ICommand
  {
    public StartExport(Guid correlationId, string streamId) : base(correlationId, streamId, $"{streamId}-{DateTime.UtcNow:yyyyMMddHHmmss}")
    {
    }

    public override string Log()
    {
      return "Starting export";
    }
  }
}
