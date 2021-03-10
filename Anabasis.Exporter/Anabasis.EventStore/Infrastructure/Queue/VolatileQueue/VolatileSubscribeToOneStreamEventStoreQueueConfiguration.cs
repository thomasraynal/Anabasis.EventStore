using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Infrastructure.Queue.VolatileQueue
{
  public class VolatileSubscribeToOneStreamEventStoreQueueConfiguration : IEventStoreQueueConfiguration
  {
    public VolatileSubscribeToOneStreamEventStoreQueueConfiguration(string streamId, UserCredentials userCredentials)
    {
      UserCredentials = userCredentials;
      StreamId = streamId;
    }

    public string StreamId { get; set; }
    public bool IgnoreUnknownEvent { get; set; } = false;
    public ISerializer Serializer { get; set; } = new DefaultSerializer();
    public UserCredentials UserCredentials { get; set; }
    public CatchUpSubscriptionFilteredSettings CatchUpSubscriptionFilteredSettings { get; set; } = CatchUpSubscriptionFilteredSettings.Default;
  }
}
