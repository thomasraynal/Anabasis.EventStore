using Anabasis.Common;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Stream
{
    public class SubscribeToAllStreamsConfiguration : IEventStoreStreamConfiguration
    {
        public SubscribeToAllStreamsConfiguration(Position position, UserCredentials? userCredentials = null)
        {
            UserCredentials = userCredentials;
            Position = position;
        }

        public Position Position { get; set; }
        public bool IgnoreUnknownEvent { get; set; } = false;
        public ISerializer Serializer { get; set; } = new DefaultSerializer();
        public UserCredentials? UserCredentials { get; set; }
        public CatchUpSubscriptionFilteredSettings CatchUpSubscriptionFilteredSettings { get; set; } = CatchUpSubscriptionFilteredSettings.Default;
        public bool DoAppCrashOnFailure { get; set; } = false;
    }
}
