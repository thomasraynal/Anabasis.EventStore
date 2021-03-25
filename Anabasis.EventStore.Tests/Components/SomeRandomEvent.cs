using Anabasis.EventStore.Event;
using System;

namespace Anabasis.Tests.Components
{
  public class SomeRandomEvent : BaseEvent
  {
    public SomeRandomEvent(Guid correlationId, string streamId = "SomeRandomEvent") : base(correlationId, streamId)
    {
    }
  }
}
