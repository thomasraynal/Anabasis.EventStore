using Anabasis.EventStore.Serialization;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Queue
{
  public class SubscribeFromEndEventStoreQueueConfiguration : IEventStoreQueueConfiguration
  {
    public SubscribeFromEndEventStoreQueueConfiguration (UserCredentials userCredentials = null)
    {
      UserCredentials = userCredentials;
    }

    public bool IgnoreUnknownEvent { get; set; } = false;
    public ISerializer Serializer { get; set; } = new DefaultSerializer();
    public UserCredentials UserCredentials { get; set; }
    public CatchUpSubscriptionFilteredSettings CatchUpSubscriptionFilteredSettings { get; set; } = CatchUpSubscriptionFilteredSettings.Default;
  }
}
