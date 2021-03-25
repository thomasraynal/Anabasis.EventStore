using Anabasis.EventStore.Event;
using System;

namespace Anabasis.EventStore.Tests.Components
{
  public class SomeRandomEvent : BaseEvent
  {
    public SomeRandomEvent(Guid correlationId, string streamId = "SomeRandomEvent") : base(correlationId, streamId)
    {
    }
  }
}
