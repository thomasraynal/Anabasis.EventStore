using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Actor
{
  public class Message
  {
    public Message(byte[] @event, Type eventType)
    {
      MessageId = Guid.NewGuid();
      Event = @event;
      EventType = eventType;
    }

    public Type EventType { get; }
    public byte[] Event { get; }
    public Guid MessageId { get; }
  }
}
