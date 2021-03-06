using Anabasis.EventStore.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Infrastructure
{
  public interface IEvent : IHaveAStreamId
  {
    Guid EventID { get; set; }
    Guid CorrelationID { get; set; }
    string Log();
  }
}
