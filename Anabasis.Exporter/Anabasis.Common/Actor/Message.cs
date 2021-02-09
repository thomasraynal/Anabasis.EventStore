using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Actor
{
  public class Message
  {
    public Message(string streamId, byte[] @event, Type eventType)
    {
      MessageId = Guid.NewGuid();
      StreamId = streamId;
      Event = @event;
      EventType = eventType;
    }

    public string CallerId { get; }
    public Type EventType { get; }
    public byte[] Event { get; }
    public Guid MessageId { get; }
    public string StreamId { get; set; }
  }
}
