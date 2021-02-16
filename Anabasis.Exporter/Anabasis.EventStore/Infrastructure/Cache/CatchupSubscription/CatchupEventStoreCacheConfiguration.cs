using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
  public class CatchupEventStoreCacheConfiguration<TKey, TCacheItem> : IEventStoreCacheConfiguration<TKey, TCacheItem> where TCacheItem : IAggregate<TKey>
  {
    public ISerializer Serializer { get; set; } = new DefaultSerializer();

    public UserCredentials UserCredentials { get; set; }

    public bool KeepAppliedEventsOnAggregate { get; set; } = false;

    public CatchUpSubscriptionSettings CatchUpSubscriptionSettings { get; set; }
  }
}
