using Anabasis.Common.Actor;
using Anabasis.Common.Mediator;
using System;
using System.Threading.Tasks;

namespace Anabasis.Common.Infrastructure
{
  public class Logger : BaseActor
  {
    public Logger(SimpleMediator simpleMediator) : base(simpleMediator)
    {
    }

    public override string StreamId => AllStream;

    protected override Task Handle(IEvent @event)
    {
      Console.WriteLine($"{@event.CorrelationID} - {@event.StreamId} - {@event.TopicId} - {@event.Log()}");

      return Task.CompletedTask;

    }
  }
}
