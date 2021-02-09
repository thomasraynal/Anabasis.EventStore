using System;

namespace Anabasis.Common.Infrastructure
{
  public interface ICommandResponse : IEvent
  {
    public Guid CommandId { get; }
    public string CallerId { get; }
  }
}
