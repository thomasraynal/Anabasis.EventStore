using EventStore.ClientAPI;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Anabasis.EventStore
{
  [Serializable]
  public class EventNotSupportedException : Exception
  {
    public EventNotSupportedException()
    {
    }

    public EventNotSupportedException(RecordedEvent ev) : base($"Event of type {ev.EventType} is not suported")
    {
    }

    public EventNotSupportedException(string message) : base(message)
    {
    }

    public EventNotSupportedException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected EventNotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }
}
