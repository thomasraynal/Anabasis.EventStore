using Anabasis.Actor;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public abstract class BaseAnabasisExporterCommand : BaseCommand, IAnabasisExporterEvent
  {
    public BaseAnabasisExporterCommand(Guid correlationId, string streamId) : base(correlationId, streamId)
    {
    }

    public string TopicId { get; set; }

    public Guid ExportId => CorrelationID;
  }
}
