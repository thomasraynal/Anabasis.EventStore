using Anabasis.EventStore.Serialization;
using Anabasis.EventStore.Shared;
using EventStore.ClientAPI.SystemData;
using System;

namespace Anabasis.EventStore.Cache
{
  public interface IEventStoreCacheConfiguration<TKey, TAggregate> where TAggregate : IAggregate<TKey>
  {
    TimeSpan IsStaleTimeSpan { get; }
    bool KeepAppliedEventsOnAggregate { get;  } 
    UserCredentials UserCredentials { get; }
    ISerializer Serializer { get; }
    public bool UseSnapshot { get;  }
  }
}
