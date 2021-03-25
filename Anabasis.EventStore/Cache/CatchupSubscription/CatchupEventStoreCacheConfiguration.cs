using Anabasis.EventStore.Serialization;
using Anabasis.EventStore.Shared;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Cache
{
  public class CatchupEventStoreCacheConfiguration<TKey, TAggregate> : IEventStoreCacheConfiguration<TKey, TAggregate> where TAggregate : IAggregate<TKey>
  {
    public CatchupEventStoreCacheConfiguration(UserCredentials userCredentials)
    {
      UserCredentials = userCredentials;
    }

    public Func<Position> GetSubscribeToAllSubscriptionCheckpoint { get; set; } = () => Position.Start;

    public ISerializer Serializer { get; set; } = new DefaultSerializer();

    public bool UseSnapshot => false;

    public UserCredentials UserCredentials { get; set; }

    public bool KeepAppliedEventsOnAggregate { get; set; } = false;

    public CatchUpSubscriptionFilteredSettings CatchUpSubscriptionFilteredSettings { get; set; } = CatchUpSubscriptionFilteredSettings.Default;

    public TimeSpan IsStaleTimeSpan { get; set; } = TimeSpan.FromHours(1);
  }
}
