using Anabasis.Common;
using Anabasis.EventStore.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Repository
{
  public interface IEventStoreAggregateRepository : IEventStoreRepository
  {
    Task Apply<TAggregate, TEvent>(TAggregate aggregate, TEvent ev, params KeyValuePair<string, string>[] extraHeaders)
      where TAggregate : IAggregate
      where TEvent : IHaveEntityId, IAggregateEvent< TAggregate>;
  }
}
