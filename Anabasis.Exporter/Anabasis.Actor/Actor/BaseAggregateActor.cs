using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure.Repository;
using System;

namespace Anabasis.Actor
{
  public abstract class BaseAggregateActor<TKey,TAggregate> : BaseActor, IDisposable
    where TAggregate : IAggregate<TKey>, new()
  {
 
    public BaseAggregateActor(IEventStoreAggregateRepository<TKey> eventStoreRepository, IEventStoreCache<TKey,TAggregate> eventStoreCache) : base(eventStoreRepository)
    {

      State = eventStoreCache;

    }

    public IEventStoreCache<TKey, TAggregate> State { get; }

  }
}
