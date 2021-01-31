using Anabasis.Common.Infrastructure;
using System;

namespace Anabasis.Common.Events
{
  public class EndExport : BaseEvent, ICommand
  {
    public EndExport(Guid correlationId) : base(correlationId)
    {
    }

    public override string Log()
    {
      return "Export has finished";
    }
  }
}

