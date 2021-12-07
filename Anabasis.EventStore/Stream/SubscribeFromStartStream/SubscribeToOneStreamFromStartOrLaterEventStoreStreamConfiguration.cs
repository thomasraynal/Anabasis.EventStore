using Anabasis.Common;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Stream
{
    public class SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration : IEventStoreStreamConfiguration
    {
        public SubscribeToOneStreamFromStartOrLaterEventStoreStreamConfiguration(string streamId, UserCredentials userCredentials = null)
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
