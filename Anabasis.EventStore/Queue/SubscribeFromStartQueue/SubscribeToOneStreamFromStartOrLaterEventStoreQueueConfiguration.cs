using Anabasis.EventStore.Serialization;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Queue
{
    public class SubscribeToOneStreamFromStartOrLaterEventStoreQueueConfiguration : IEventStoreQueueConfiguration
    {
        public SubscribeToOneStreamFromStartOrLaterEventStoreQueueConfiguration(string streamId, UserCredentials userCredentials = null)
        {
            UserCredentials = userCredentials;
            StreamId = streamId;
            EventStreamPosition = StreamPosition.Start;
        }

        public int EventStreamPosition { get; set; } 
        public string StreamId { get; set; }
        public bool IgnoreUnknownEvent { get; set; } = false;
        public ISerializer Serializer { get; set; } = new DefaultSerializer();
        public UserCredentials UserCredentials { get; set; }
        public CatchUpSubscriptionFilteredSettings CatchUpSubscriptionFilteredSettings { get; set; } = CatchUpSubscriptionFilteredSettings.Default;
    }
}
