using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Infrastructure
{
  public abstract class BaseEvent : IEvent
  {

    public BaseEvent(Guid correlationId, string streamId, string topicId)
    {
      EventID = Guid.NewGuid();
      TopicId = topicId;
      StreamId = streamId;
      CorrelationID = correlationId;
    }

    public Guid ExportId => CorrelationID;
    public Guid EventID { get; set; }
    public string StreamId { get; set; }
    public string TopicId { get; set; }
    public Guid CorrelationID { get; set; }

    public abstract string Log();
  }
}
