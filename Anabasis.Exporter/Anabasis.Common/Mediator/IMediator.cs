using Anabasis.Common.Actor;

namespace Anabasis.Common.Mediator
{
  public interface IMediator: IEventEmitter
  {
    void Send(Message message);
  }
}
