using Anabasis.Common.Actor;
using Anabasis.Common.Mediator;
using System;
using System.Threading.Tasks;

namespace Anabasis.Common.Infrastructure
{
  [AlwaysConsume]
  public class Logger : BaseActor
  {
    public Logger(IMediator simpleMediator) : base(simpleMediator)
    {
    }

    public override string StreamId => StreamIds.AllStream;

    public Task Handle(IEvent @event)
    {
      Console.WriteLine($"{@event.CorrelationID} - {@event.StreamId} - {@event.TopicId} - {@event.Log()}");

      return Task.CompletedTask;

    }
  }
}
