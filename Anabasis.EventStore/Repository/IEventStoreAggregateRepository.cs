using Anabasis.EventStore.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Repository
{
  public interface IEventStoreAggregateRepository<TKey> : IEventStoreRepository
  {
    Task Apply<TEntity, TEvent>(TEntity aggregate, TEvent ev, params KeyValuePair<string, string>[] extraHeaders)
      where TEntity : IAggregate<TKey>
      where TEvent : IEntity<TKey>, IMutation<TKey, TEntity>;
    //Task<TAggregate> GetById<TAggregate>(TKey id, bool loadEvents = false) where TAggregate : IAggregate<TKey>, new();


  }
}
