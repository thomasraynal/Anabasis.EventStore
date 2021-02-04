using Anabasis.Common.Actor;
using Anabasis.Common.Infrastructure;

namespace Anabasis.Common.Mediator
{
  public interface IMediator
  {
    void Emit(IEvent @event);
  }
}
