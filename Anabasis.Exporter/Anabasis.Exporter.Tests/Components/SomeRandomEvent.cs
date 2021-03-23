using Anabasis.Actor;
using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure;
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
