using Anabasis.Common.Actor;
using Anabasis.Common.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Anabasis.Common.Mediator
{
  public interface IMediator
  {
    void Emit(IEvent @event);
  }
}
