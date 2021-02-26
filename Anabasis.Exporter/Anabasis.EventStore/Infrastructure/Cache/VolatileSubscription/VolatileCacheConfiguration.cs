using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;

namespace Anabasis.EventStore.Infrastructure.Cache.VolatileSubscription
{
  public class VolatileCacheConfiguration<TKey, TCacheItem> : IEventStoreCacheConfiguration<TKey, TCacheItem> where TCacheItem : IAggregate<TKey>, new()
  {
    public VolatileCacheConfiguration(UserCredentials userCredentials)
    {
      UserCredentials = userCredentials;
    }

    public ISerializer Serializer { get; set; } = new DefaultSerializer();
    public UserCredentials UserCredentials { get; set; }
    public CatchUpSubscriptionFilteredSettings CatchUpSubscriptionFilteredSettings { get; set; } = CatchUpSubscriptionFilteredSettings.Default;
    public bool KeepAppliedEventsOnAggregate { get; set; } = false;
    public TimeSpan IsStaleTimeSpan { get; set; } = TimeSpan.MaxValue;
  }
}
