using Anabasis.Common.Actor;
using Anabasis.Common.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Anabasis.Common.Mediator
{
  public interface IMediator
  {
    void Emit(IEvent @event);
    Task Send<TCommandResult>(ICommand command, TimeSpan? timeout = null)
    where TCommandResult : ICommandResponse;
  }
}
