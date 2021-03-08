using Anabasis.Actor;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public abstract class BaseAnabasisExporterEvent : BaseEvent, IAnabasisExporterEvent
  {
    public BaseAnabasisExporterEvent(Guid correlationId, string streamId, string topicId) : base(correlationId, streamId)
    {
      TopicId = topicId;
    }

    public string TopicId { get; set; }
    public Guid ExportId => CorrelationID;
  }
}
