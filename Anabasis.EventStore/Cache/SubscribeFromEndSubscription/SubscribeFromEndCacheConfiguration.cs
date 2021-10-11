using Anabasis.EventStore.Serialization;
using Anabasis.EventStore.Shared;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;

namespace Anabasis.EventStore.Cache
{
  public class SubscribeFromEndCacheConfiguration<TKey, TAggregate> : IEventStoreCacheConfiguration<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
  {
    public SubscribeFromEndCacheConfiguration(UserCredentials userCredentials= null)
    {
      UserCredentials = userCredentials;
    }

    public ISerializer Serializer { get; set; } = new DefaultSerializer();
    public UserCredentials UserCredentials { get; set; }
    public CatchUpSubscriptionFilteredSettings CatchUpSubscriptionFilteredSettings { get; set; } = CatchUpSubscriptionFilteredSettings.Default;
    public bool KeepAppliedEventsOnAggregate { get; set; } = false;
    public TimeSpan IsStaleTimeSpan { get; set; } = TimeSpan.FromHours(1);
    public bool UseSnapshot => false;
  }
}
