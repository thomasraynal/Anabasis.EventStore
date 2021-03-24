using Anabasis.Actor;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Events
{
  public abstract class BaseAnabasisExporterCommandResponse : BaseCommandResponse, IAnabasisExporterEvent
  {
    public BaseAnabasisExporterCommandResponse(Guid commandId, Guid correlationId, string streamId) : base(commandId, correlationId, streamId)
    {
    }

    public Guid ExportId => CorrelationID;

    public abstract string Log();
  }
}
