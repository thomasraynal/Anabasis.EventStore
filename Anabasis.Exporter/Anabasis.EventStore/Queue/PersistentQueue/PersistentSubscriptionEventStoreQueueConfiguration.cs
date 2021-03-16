using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Infrastructure.Queue.PersistentQueue
{
  public class PersistentSubscriptionEventStoreQueueConfiguration : IEventStoreQueueConfiguration
  {
    public PersistentSubscriptionEventStoreQueueConfiguration(string streamId, string groupId, UserCredentials userCredentials)
    {
      StreamId = streamId;
      GroupId = groupId;
      UserCredentials = userCredentials;
    }

    public string StreamId { get; set; }
    public string GroupId { get; set; }
    public ISerializer Serializer { get; set; } = new DefaultSerializer();
    public UserCredentials UserCredentials { get; set; }
    public int BufferSize { get; set; } = EventStorePersistentSubscriptionBase.DefaultBufferSize;
    public bool AutoAck { get; set; } = true;
    public bool IgnoreUnknownEvent => true;
  }
}

