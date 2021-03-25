using Anabasis.EventStore;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Shared;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Actor
{
  public interface IAggregateActor<TKey, TAggregate> : IActor where TAggregate : IAggregate<TKey>, new() 
  {
    IEventStoreCache<TKey, TAggregate> State { get; }

    Task EmitEntityEvent<TEvent>(TEvent @event, params KeyValuePair<string, string>[] extraHeaders) where TEvent : IEntity<TKey>;
  }
}
