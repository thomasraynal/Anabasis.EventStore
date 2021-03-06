using System;

namespace Anabasis.Actor
{
  public abstract class BaseCommand : BaseEvent, ICommand
  {
    public BaseCommand(Guid correlationId, string streamId) : base(correlationId, streamId)
    {
      StreamId = streamId;
    }

    public BaseCommand()
    {
    }
  }
}
