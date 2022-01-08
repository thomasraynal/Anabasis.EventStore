using Anabasis.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Event
{
  public abstract class BaseCommandResponse : BaseEvent, ICommandResponse
  {
    public BaseCommandResponse(Guid commandId, Guid correlationId, string streamId) : base(correlationId, streamId)
    {
      CommandId = commandId;
    }

    [JsonProperty]
    public Guid CommandId { get; internal set; }
  }
}
