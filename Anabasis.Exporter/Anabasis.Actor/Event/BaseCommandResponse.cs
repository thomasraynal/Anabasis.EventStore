using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Actor
{
  public abstract class BaseCommandResponse : BaseEvent, ICommandResponse
  {
    public BaseCommandResponse(Guid commandId, Guid correlationId, string streamId) : base(correlationId, streamId)
    {
      CommandId = commandId;
    }

    public BaseCommandResponse()
    {
    }

    public Guid CommandId { get; set; }
  }
}
