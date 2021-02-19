using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Infrastructure.Cache
{
  public class SingleStreamCatchupEventStoreCacheConfiguration<TKey, TCacheItem> : IEventStoreCacheConfiguration<TKey, TCacheItem> where TCacheItem : IAggregate<TKey>
  {
    public ISerializer Serializer { get; set; } = new DefaultSerializer();

    public string StreamId { get; set; }

    public UserCredentials UserCredentials { get; set; }

    public bool KeepAppliedEventsOnAggregate { get; set; } = false;

    public CatchUpSubscriptionSettings CatchUpSubscriptionSettings { get; set; } = CatchUpSubscriptionSettings.Default;

    public TimeSpan IsStaleTimeSpan { get; set; } = TimeSpan.MaxValue;
  }
}
