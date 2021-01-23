using Anabasis.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public class EndExport : BaseEvent, ICommand
  {
    public EndExport(Guid correlationId) : base(correlationId)
    {
    }

    public override string Log()
    {
      return "Export ended";
    }
  }
}

