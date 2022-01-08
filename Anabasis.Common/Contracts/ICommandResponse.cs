using System;

namespace Anabasis.Common
{
  public interface ICommandResponse : IEvent
  {
    public Guid CommandId { get; }
  }
}
