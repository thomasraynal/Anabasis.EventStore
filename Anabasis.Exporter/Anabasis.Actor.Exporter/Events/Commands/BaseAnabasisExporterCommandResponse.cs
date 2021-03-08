using Anabasis.Actor;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public abstract class BaseAnabasisExporterCommandResponse : BaseCommandResponse, IAnabasisExporterEvent
  {
    public BaseAnabasisExporterCommandResponse(Guid commandId, Guid correlationId, string streamId, string topicId) : base(commandId, correlationId, streamId)
    {
      TopicId = topicId;
    }

    public string TopicId { get; set; }

    public Guid ExportId => CorrelationID;
  }
}
