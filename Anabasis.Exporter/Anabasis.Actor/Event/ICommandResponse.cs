using System;

namespace Anabasis.Actor
{
  public interface ICommandResponse : IActorEvent
  {
    public Guid CommandId { get; }
  }
}
