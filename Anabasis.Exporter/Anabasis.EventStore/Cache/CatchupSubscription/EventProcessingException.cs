using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Anabasis.EventStore.Cache.CatchupSubscription
{
  [Serializable]
  public class EventProcessingException : Exception
  {
    public EventProcessingException()
    {
    }

    public EventProcessingException(string message) : base(message)
    {
    }

    public EventProcessingException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected EventProcessingException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
  }
}
