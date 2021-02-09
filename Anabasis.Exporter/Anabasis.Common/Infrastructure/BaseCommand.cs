using System;

namespace Anabasis.Common.Infrastructure
{
  public abstract class BaseCommand : BaseEvent, ICommand
  {
    public BaseCommand(Guid commandId, Guid correlationId, string streamId, string topicId) : base(correlationId, streamId, topicId)
    {
      CommandId = commandId;
    }

    public string CallerId { get; internal set; }
    public Guid CommandId { get; }
  }
}
