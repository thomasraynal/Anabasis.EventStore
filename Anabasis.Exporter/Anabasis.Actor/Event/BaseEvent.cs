using Anabasis.EventStore.Infrastructure;
using Newtonsoft.Json;
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

    [JsonProperty]
    public Guid EventID { get; internal set; }
    [JsonProperty]
    public Guid CorrelationID { get; internal set; }
    [JsonProperty]
    public string StreamId { get; internal set; }

    public abstract string Log();
  }
}
