using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure.Repository;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.Actor
{
  public abstract class BaseAggregateActor<TKey, TAggregate> : BaseActor, IDisposable, IAggregateActor<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
  {

    public BaseAggregateActor(IEventStoreAggregateRepository<TKey> eventStoreRepository, IEventStoreCache<TKey, TAggregate> eventStoreCache) : base(eventStoreRepository)
    {
      _eventStoreAggregateRepository = eventStoreRepository;
      State = eventStoreCache;
    }

    private readonly IEventStoreAggregateRepository<TKey> _eventStoreAggregateRepository;

    public IEventStoreCache<TKey, TAggregate> State { get; }

    public async Task EmitEntityEvent<TEvent>(TEvent @event, params KeyValuePair<string, string>[] extraHeaders) where TEvent : IEntity<TKey>
    {
      await _eventStoreAggregateRepository.Emit(@event, extraHeaders);
    }

  }
}
