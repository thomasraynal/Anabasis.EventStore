using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.Actor
{
  public abstract class BaseAggregateActor<TKey,TEventStoreCache, TAggregate> : BaseActor, IDisposable
  {

    private readonly TEventStoreCache _eventStoreCache;
    private readonly IEventStoreRepository<TKey> _eventStoreRepository;
    private readonly IDisposable _stateSubscriptionDisposable;

    protected BaseAggregateActor(IEventStoreRepository<TKey> eventStoreRepository, TEventStoreCache eventStoreCache)
    {
      _eventStoreCache = eventStoreCache;
      _eventStoreRepository = eventStoreRepository;
    }

  

    public bool CanConsume(IActorEvent @event)
    {
      return null != _messageHandlerInvokerCache.GetMethodInfo(GetType(), @event.GetType());
    }



    public void Dispose()
    {
      _stateSubscriptionDisposable.Dispose();
    }
  }
}
