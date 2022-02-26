using Anabasis.Common;
using Anabasis.EventStore.Shared;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;

namespace Anabasis.EventStore.Cache
{
    public class AllStreamsCatchupCacheConfiguration< TAggregate> : IEventStoreCacheConfiguration< TAggregate> where TAggregate : IAggregate
    {
        public Position Checkpoint { get; set; } = Position.Start;

        public AllStreamsCatchupCacheConfiguration(Position checkpoint)
        {
            Checkpoint = checkpoint;
        }

        public bool DoAppCrashIfSubscriptionFail { get; set; }
        public TimeSpan IsStaleTimeSpan { get; set; } = TimeSpan.FromHours(1);
        public bool KeepAppliedEventsOnAggregate { get; set; } = false;
        public UserCredentials UserCredentials { get; set; }
        public ISerializer Serializer { get; set; } = new DefaultSerializer();
        public bool UseSnapshot { get; set; }
        public CatchUpSubscriptionFilteredSettings CatchUpSubscriptionFilteredSettings { get; set; } = CatchUpSubscriptionFilteredSettings.Default;
    }
}
