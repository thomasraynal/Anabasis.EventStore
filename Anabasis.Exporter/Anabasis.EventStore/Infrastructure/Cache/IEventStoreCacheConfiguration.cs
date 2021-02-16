using EventStore.ClientAPI.SystemData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
  public interface IEventStoreCacheConfiguration<TKey, TCacheItem> where TCacheItem : IAggregate<TKey>
  {
    bool KeepAppliedEventsOnAggregate { get;  } 
    UserCredentials UserCredentials { get; }
    ISerializer Serializer { get; }
  }
}
