using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Infrastructure
{
  public interface IEvent
  {
    Guid EventID { get; }
    Guid CorrelationID { get; }
    string Log();
  }
}
