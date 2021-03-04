using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure;
using System;

namespace Anabasis.Tests.Components
{
  public class SomeRandomEvent : IEvent
  {
    private string _streamName;

    public SomeRandomEvent()
    {
    }

    public SomeRandomEvent(string streamName)
    {
      _streamName = streamName;
    }

    public Guid EventID { get; set; }

    public Guid CorrelationID { get; set; }

    public string GetStreamName()
    {
      return _streamName ?? nameof(SomeRandomEvent);
    }

    public string Log()
    {
      return nameof(SomeRandomEvent);
    }
  }
}
