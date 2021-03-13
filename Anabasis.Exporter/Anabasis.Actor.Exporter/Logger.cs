using Anabasis.Actor;
using Anabasis.Actor.Exporter;
using Anabasis.Common.Events;
using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Anabasis.Common.Infrastructure
{

  public class Logger : BaseActor
  {
    public Logger(IEventStoreRepository eventStoreRepository) : base(eventStoreRepository)
    {
    }

    [EventSink]
    public Task Handle(IEvent @event)
    {

      var exportedEvent = @event as IAnabasisExporterEvent;

      if (null != exportedEvent)
      {
        Console.WriteLine($"{@event.CorrelationID} - {exportedEvent.StreamId} - {exportedEvent.Log()}");
      }

      return Task.CompletedTask;

    }
  }
}
