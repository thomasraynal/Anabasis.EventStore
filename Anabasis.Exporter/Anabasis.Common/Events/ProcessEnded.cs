using Anabasis.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public class ProcessEnded : BaseEvent
  {
    public ProcessEnded(Guid correlationId) : base(correlationId)
    {
    }

    public override string Log()
    {
      return "Process has ended";
    }
  }
}

