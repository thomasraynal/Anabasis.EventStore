using System;

namespace Anabasis.Actor
{
  public abstract class BaseCommand : BaseEvent, ICommand
  {
    public BaseCommand(Guid correlationId, string streamId, string topicId) : base(correlationId, streamId, topicId)
    {
    }

  }
}
