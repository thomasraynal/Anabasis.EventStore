using Anabasis.EventStore.Infrastructure;
using System;
namespace Anabasis.Actor
{
  public abstract class BaseEvent : IEvent
  {

    private readonly string _streamId;

    public BaseEvent(Guid correlationId, string streamId, string topicId)
    {
      EventID = Guid.NewGuid();
      TopicId = topicId;
      CorrelationID = correlationId;

      _streamId = streamId;

    }
    public BaseEvent()
    {
    }

    public Guid EventID { get; set; }
    public string TopicId { get; set; }
    public Guid CorrelationID { get; set; }

    public string GetStreamName()
    {
      return _streamId;
    }

    public abstract string Log();
  }
}
