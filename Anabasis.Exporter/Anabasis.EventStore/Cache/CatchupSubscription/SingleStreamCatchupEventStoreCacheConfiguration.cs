using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Infrastructure.Cache.CatchupSubscription
{
  public class SingleStreamCatchupEventStoreCacheConfiguration<TKey, TAggregate> : IEventStoreCacheConfiguration<TKey, TAggregate> where TAggregate : IAggregate<TKey>
  {
    public SingleStreamCatchupEventStoreCacheConfiguration(string streamId, UserCredentials userCredentials)
    {
      StreamId = streamId;
      UserCredentials = userCredentials;
    }

    public ISerializer Serializer { get; set; } = new DefaultSerializer();

    public string StreamId { get; set; }

    public bool UseSnapshot { get; set; }

    public UserCredentials UserCredentials { get; set; }

    public bool KeepAppliedEventsOnAggregate { get; set; } = false;

    public CatchUpSubscriptionSettings CatchUpSubscriptionSettings { get; set; } = CatchUpSubscriptionSettings.Default;

    public TimeSpan IsStaleTimeSpan { get; set; } = TimeSpan.FromHours(1);
  }
}
