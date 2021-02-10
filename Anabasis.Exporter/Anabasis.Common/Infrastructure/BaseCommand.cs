using System;

namespace Anabasis.Common.Infrastructure
{
  public abstract class BaseCommand : BaseEvent, ICommand
  {
    public BaseCommand(Guid correlationId, string streamId, string topicId) : base(correlationId, streamId, topicId)
    {
    }

  }
}
