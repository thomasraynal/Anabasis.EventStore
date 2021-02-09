using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Infrastructure
{
  public abstract class BaseCommandResponse : BaseEvent, ICommandResponse
  {
    public BaseCommandResponse(Guid correlationId, string streamId, string topicId) : base(correlationId, streamId, topicId)
    {
    }

    public string CallerId { get; internal set; }

    public Guid CommandId { get;  set; }
  }
}
