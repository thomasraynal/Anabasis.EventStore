using Anabasis.Common;
using Anabasis.EventStore.Stream;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventStore2.Configuration
{
    public class SubscribeToOneStreamConfiguration : IEventStoreStreamConfiguration
    {
        public SubscribeToOneStreamConfiguration(string streamId, long eventStreamPosition = 0, UserCredentials? userCredentials = null)
        {
            UserCredentials = userCredentials;
            StreamId = streamId;
            EventStreamPosition = eventStreamPosition;
        }

        public long EventStreamPosition { get; set; }
        public string StreamId { get; set; }
        public bool IgnoreUnknownEvent { get; set; } = false;
        public ISerializer Serializer { get; set; } = new DefaultSerializer();
        public UserCredentials? UserCredentials { get; set; }
        public CatchUpSubscriptionFilteredSettings CatchUpSubscriptionFilteredSettings { get; set; } = CatchUpSubscriptionFilteredSettings.Default;
        public bool DoAppCrashOnFailure { get; set; } = false;
    }
}
