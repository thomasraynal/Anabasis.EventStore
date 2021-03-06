using Anabasis.EventStore.Infrastructure;
using System;
namespace Anabasis.Actor
{
  public abstract class BaseEvent : IEvent
  {

    private readonly string _streamId;

    public BaseEvent(Guid correlationId, string streamId)
    {
      EventID = Guid.NewGuid();
      CorrelationID = correlationId;

      _streamId = streamId;

    }
    public BaseEvent()
    {
    }
    public Guid EventID { get; set; }
    public Guid CorrelationID { get; set; }

    public virtual string GetStreamName()
    {
      return _streamId;
    }

    public abstract string Log();
  }
}
