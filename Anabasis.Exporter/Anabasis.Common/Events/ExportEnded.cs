using Anabasis.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public class ExportEnded : BaseEvent
  {
    public ExportEnded(Guid correlationId) : base(correlationId)
    {
    }

    public override string Log()
    {
      return "Export ended";
    }
  }
}
