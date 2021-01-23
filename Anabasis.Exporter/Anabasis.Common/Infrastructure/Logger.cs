using Anabasis.Common.Actor;
using Anabasis.Common.Events;
using Anabasis.Common.Mediator;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Common.Infrastructure
{
  public class Logger : BaseActor
  {
    public Logger(SimpleMediator simpleMediator) : base(simpleMediator)
    {
    }

    protected override Task Handle(IEvent @event)
    {
      Console.WriteLine($"{@event.CorrelationID} - {@event.Log()}");

      return Task.CompletedTask;

    }
  }
}
