using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure;
using System;

namespace Anabasis.Tests.Components
{
  public class SomeRandomEvent : IEvent
  {
    public SomeRandomEvent()
    {
    }

    public Guid EventID { get; set; }

    public Guid CorrelationID { get; set; }

    public string GetStreamName()
    {
      return nameof(SomeRandomEvent);
    }

    public string Log()
    {
      return nameof(SomeRandomEvent);
    }
  }
}
