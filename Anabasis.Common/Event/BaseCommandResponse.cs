using Newtonsoft.Json;
using System;

namespace Anabasis.Common
{
    public abstract class BaseCommandResponse : BaseEvent, ICommandResponse
  {
    public BaseCommandResponse(string entityId, Guid commandId, Guid correlationId) : base(entityId, correlationId, commandId)
    {
      CommandId = commandId;
    }

    [JsonProperty]
    public Guid CommandId { get; internal set; }
  }
}
