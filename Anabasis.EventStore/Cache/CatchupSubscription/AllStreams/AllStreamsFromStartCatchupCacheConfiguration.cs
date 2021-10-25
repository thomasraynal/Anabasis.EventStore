using Anabasis.EventStore.Serialization;
using Anabasis.EventStore.Shared;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;

namespace Anabasis.EventStore.Cache
{
    public class AllStreamsFromStartCatchupCacheConfiguration<TKey, TAggregate> : IEventStoreCacheConfiguration<TKey, TAggregate> where TAggregate : IAggregate<TKey>
    {
        public Position Checkpoint = Position.Start;
        public TimeSpan IsStaleTimeSpan { get; set; } = TimeSpan.FromHours(1);
        public bool KeepAppliedEventsOnAggregate { get; set; } = false;
        public UserCredentials UserCredentials { get; set; }
        public ISerializer Serializer { get; set; } = new DefaultSerializer();
        public bool UseSnapshot { get; set; }
        public CatchUpSubscriptionFilteredSettings CatchUpSubscriptionFilteredSettings { get; set; } = CatchUpSubscriptionFilteredSettings.Default;
        public string[] StreamIds => new string[0];
        public bool IsSubscribeAll => true;
    }
}
