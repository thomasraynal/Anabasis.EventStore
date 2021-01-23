using Anabasis.Common.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Actor
{
  public interface IEventEmitter
  {
    void Emit(IEvent @event);

  }
}
