using Anabasis.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public class StartExport : BaseEvent, ICommand
  {
    public StartExport(Guid exportId) : base(exportId)
    {
    }

    public override string Log()
    {
      return "Export started";
    }
  }
}
