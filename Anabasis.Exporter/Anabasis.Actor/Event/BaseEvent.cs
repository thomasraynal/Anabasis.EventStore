using Anabasis.EventStore.Infrastructure;
using System;
namespace Anabasis.Actor
{
  public abstract class BaseEvent : IEvent
  {

    public BaseEvent(Guid correlationId, string streamId)
    {
      EventID = Guid.NewGuid();

      CorrelationID = correlationId;
      StreamId = streamId;
    }

    public BaseEvent()
    {
    }

    public Guid EventID { get; set; }
    public Guid CorrelationID { get; set; }
    public string StreamId { get; set; }

    public abstract string Log();
  }
}
