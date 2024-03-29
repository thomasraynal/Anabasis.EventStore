using Anabasis.Common;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace Anabasis.EventStore.Stream
{
    public class PersistentSubscriptionStreamConfiguration : IEventStoreStreamConfiguration
    {
        public PersistentSubscriptionStreamConfiguration(string streamId, string groupId, UserCredentials? userCredentials = null)
        {
            StreamId = streamId;
            GroupId = groupId;
            UserCredentials = userCredentials;
        }

        public string StreamId { get; set; }
        public string GroupId { get; set; }
        public ISerializer Serializer { get; set; } = new DefaultSerializer();
        public UserCredentials? UserCredentials { get; set; }
        public int BufferSize { get; set; } = EventStorePersistentSubscriptionBase.DefaultBufferSize;
        public bool AutoAck { get; set; } = true;
        public bool IgnoreUnknownEvent => true;
        public bool DoAppCrashOnFailure { get; set; } = false;
    }
}

