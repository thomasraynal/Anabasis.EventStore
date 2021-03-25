using Anabasis.Common.Events;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Shared;
using System;
using System.Threading.Tasks;

namespace Anabasis.Actor.Exporter.Exporters
{
  public abstract class BaseAnabasisActor : BaseActor
  {
    public BaseAnabasisActor(IEventStoreRepository eventStoreRepository) : base(eventStoreRepository)
    {
    }

    public async override Task OnError(IEvent source, Exception exception)
    {
      await Emit(new ErrorThrowed(source.CorrelationID, source.StreamId, exception));
    }
  }
}
