using Anabasis.Common;
using System;

namespace Anabasis.EventStore.Tests
{
    public class SomeRandomEvent : BaseEvent
  {
    public SomeRandomEvent(Guid correlationId, string streamId = "SomeRandomEvent") : base(correlationId, streamId)
    {
    }
  }
}
