using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;

namespace Anabasis.EventStore.Infrastructure.Cache
{
  public class PersistentSubscriptionCacheConfiguration<TKey, TAggregate> : IEventStoreCacheConfiguration<TKey, TAggregate> where TAggregate : IAggregate<TKey>, new()
  {
    public PersistentSubscriptionCacheConfiguration(string streamId, string groupId, UserCredentials userCredentials)
    {
      StreamId = streamId;
      GroupId = groupId;
      UserCredentials = userCredentials;
    }

    public string StreamId { get; set; }
    public string GroupId { get; set; }
    public int BufferSize { get; set; } = EventStorePersistentSubscriptionBase.DefaultBufferSize;
    public bool AutoAck { get; set; } = true;
    public ISerializer Serializer { get; set; } = new DefaultSerializer();
    public UserCredentials UserCredentials { get; set; }
    public bool KeepAppliedEventsOnAggregate { get; set; } = false;
    public TimeSpan IsStaleTimeSpan { get; set; } = TimeSpan.FromHours(1);
  }
}
