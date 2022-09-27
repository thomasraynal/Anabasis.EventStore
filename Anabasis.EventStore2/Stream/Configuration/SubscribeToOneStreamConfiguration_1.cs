using Anabasis.Common;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace Anabasis.EventStore.Stream
{
    public class SubscribeToOneStreamConfiguration : IEventStoreStreamConfiguration
    {
        public SubscribeToOneStreamConfiguration(string streamId, long? eventStreamPosition = null, UserCredentials? userCredentials = null)
        {
            UserCredentials = userCredentials;
            StreamId = streamId;
            EventStreamPosition = eventStreamPosition;
        }

        public long? EventStreamPosition { get; set; } 
        public string StreamId { get; set; }
        public bool IgnoreUnknownEvent { get; set; } = false;
        public ISerializer Serializer { get; set; } = new DefaultSerializer();
        public UserCredentials? UserCredentials { get; set; }
        public CatchUpSubscriptionFilteredSettings CatchUpSubscriptionFilteredSettings { get; set; } = CatchUpSubscriptionFilteredSettings.Default;
        public bool DoAppCrashOnFailure { get; set; } = false;
    }
}
