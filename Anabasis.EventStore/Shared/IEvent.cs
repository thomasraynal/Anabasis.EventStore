using System;

namespace Anabasis.EventStore.Shared
{
  public interface IEvent : IHaveAStreamId
  {
    Guid EventID { get;  }
    Guid CorrelationID { get;  }
  }
}
