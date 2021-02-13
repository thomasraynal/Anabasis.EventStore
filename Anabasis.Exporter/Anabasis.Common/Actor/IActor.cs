using Anabasis.Common.Infrastructure;
using Anabasis.Common.Mediator;
using System;
using System.Threading.Tasks;

namespace Anabasis.Common.Actor
{
  public interface IActor: IDispatchQueue<IEvent>
  {
    string ActorId { get; }
    string StreamId { get; }
    bool CanConsume(IEvent message);
  }
}
