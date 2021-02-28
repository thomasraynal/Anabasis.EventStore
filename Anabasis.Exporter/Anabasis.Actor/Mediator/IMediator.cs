

namespace Anabasis.Actor
{
  public interface IMediator
  {
    void Emit(IActorEvent @event);
  }
}
