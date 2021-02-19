using EventStore.ClientAPI.SystemData;
using System;

namespace Anabasis.EventStore
{
  public interface IEventStoreCacheConfiguration<TKey, TCacheItem> where TCacheItem : IAggregate<TKey>
  {
    TimeSpan IsStaleTimeSpan { get; }
    bool KeepAppliedEventsOnAggregate { get;  } 
    UserCredentials UserCredentials { get; }
    ISerializer Serializer { get; }
  }
}
