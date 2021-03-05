using Anabasis.EventStore.Infrastructure;
using System;

namespace Anabasis.Actor
{
  public interface ICommandResponse : IEvent
  {
    public Guid CommandId { get; }
  }
}
