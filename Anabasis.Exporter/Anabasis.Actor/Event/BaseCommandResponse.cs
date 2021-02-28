using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Actor
{
  public abstract class BaseCommandResponse : BaseEvent, ICommandResponse
  {
    public BaseCommandResponse(Guid commandId, Guid correlationId, string streamId, string topicId) : base(correlationId, streamId, topicId)
    {
      CommandId = commandId;
    }

    public Guid CommandId { get; internal set; }
  }
}
