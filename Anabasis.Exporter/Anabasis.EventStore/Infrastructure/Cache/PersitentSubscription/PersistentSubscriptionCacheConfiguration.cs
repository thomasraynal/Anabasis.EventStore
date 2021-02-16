using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Infrastructure.Cache
{
  public class PersistentSubscriptionCacheConfiguration<TKey, TCacheItem> : IEventStoreCacheConfiguration<TKey, TCacheItem> where TCacheItem : IAggregate<TKey>, new()
  {
    public string StreamId { get; set; }
    public string GroupId { get; set; }
    public ISerializer Serializer { get; set; }
    public PersistentSubscriptionSettings PersistentSubscriptionSettings { get; set; }
    public UserCredentials UserCredentials { get; set; }

    public bool KeepAppliedEventsOnAggregate { get; set; } = false;
  }
}
