using EventStore.ClientAPI;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace Anabasis.EventStore
{
    public class EventNotSupportedException : Exception
    {
        public EventNotSupportedException()
        {
        }

        public EventNotSupportedException(RecordedEvent ev) : base($"{ev.EventType}")
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
