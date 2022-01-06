using Anabasis.Common;
using Anabasis.EventStore.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Repository
{
  public interface IEventStoreAggregateRepository : IEventStoreRepository
  {
    Task Apply<TEntity, TEvent>(TEntity aggregate, TEvent ev, params KeyValuePair<string, string>[] extraHeaders)
      where TEntity : IAggregate
      where TEvent : IEntity, IMutation< TEntity>;
  }
}
