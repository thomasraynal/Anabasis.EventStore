using Anabasis.Common.Actor;
using Anabasis.Common.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Anabasis.Common.Mediator
{
  public interface IMediator
  {
    void Emit(IEvent @event);
    Task Send<TCommand, TCommandResult>(TCommand command, TimeSpan? timeout = null)
    where TCommand : BaseCommand // use a wrapper
    where TCommandResult : ICommandResponse;
  }
}
