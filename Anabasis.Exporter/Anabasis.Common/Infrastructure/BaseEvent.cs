using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Infrastructure
{
  public abstract class BaseEvent : IEvent
  {
    public BaseEvent(Guid correlationId)
    {
      EventID = Guid.NewGuid();
      CorrelationID = correlationId;
    }

    public Guid EventID { get; set; }
    public Guid CorrelationID { get; set; }

    public abstract string Log();
  }
}
