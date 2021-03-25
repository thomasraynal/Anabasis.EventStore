using Anabasis.EventStore.Shared;
using System;

namespace Anabasis.EventStore.Event
{
  public interface ICommandResponse : IEvent
  {
    public Guid CommandId { get; }
  }
}
