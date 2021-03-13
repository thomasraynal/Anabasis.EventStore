using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Infrastructure.Queue.SubscribeFromEndQueue
{
  public class SubscribeFromEndEventStoreQueueConfiguration : IEventStoreQueueConfiguration
  {
    public SubscribeFromEndEventStoreQueueConfiguration (UserCredentials userCredentials)
    {
      UserCredentials = userCredentials;
    }

    public bool IgnoreUnknownEvent { get; set; } = false;
    public ISerializer Serializer { get; set; } = new DefaultSerializer();
    public UserCredentials UserCredentials { get; set; }
    public CatchUpSubscriptionFilteredSettings CatchUpSubscriptionFilteredSettings { get; set; } = CatchUpSubscriptionFilteredSettings.Default;
  }
}
