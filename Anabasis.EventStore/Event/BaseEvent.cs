using Anabasis.EventStore.Shared;
using Newtonsoft.Json;
using System;

namespace Anabasis.EventStore.Event
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

  }
}
