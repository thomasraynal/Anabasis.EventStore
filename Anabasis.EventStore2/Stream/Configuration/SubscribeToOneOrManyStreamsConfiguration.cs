using Anabasis.Common;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;

namespace Anabasis.EventStore.Stream
{
    public class SubscribeToOneOrManyStreamsConfiguration : IEventStoreStreamConfiguration
    {
        public SubscribeToOneOrManyStreamsConfiguration(string[] streamIds, int eventStreamPosition = StreamPosition.Start, UserCredentials? userCredentials = null)
        {
            UserCredentials = userCredentials;
            StreamIds = streamIds;
            EventStreamPosition = eventStreamPosition;
        }

        public int EventStreamPosition { get; set; } 
        public string[] StreamIds { get; set; }
        public bool IgnoreUnknownEvent { get; set; } = false;
        public ISerializer Serializer { get; set; } = new DefaultSerializer();
        public UserCredentials? UserCredentials { get; set; }
        public CatchUpSubscriptionFilteredSettings CatchUpSubscriptionFilteredSettings { get; set; } = CatchUpSubscriptionFilteredSettings.Default;
        public bool DoAppCrashOnFailure { get; set; } = false;
    }
}
