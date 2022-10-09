using Anabasis.Common;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;

namespace Anabasis.EventStore.Cache
{
    public class MultipleStreamsCatchupCacheConfiguration : IAggregateCacheConfiguration
    {
        public MultipleStreamsCatchupCacheConfiguration(params string[] streamIds)
        {
            if (null == streamIds || streamIds.Length == 0)
            {
                throw new ArgumentException($"{streamIds} should not be empty");
            }

            StreamIds = streamIds;
        }

        public string[] StreamIds { get; set; }
        public TimeSpan IsStaleTimeSpan { get; set; } = TimeSpan.FromHours(1);
        public bool CrashAppIfSubscriptionFail { get; set; } = false;
        public bool KeepAppliedEventsOnAggregate { get; set; } = false;
        public UserCredentials? UserCredentials { get; set; }
        public ISerializer Serializer { get; set; } = new DefaultSerializer();
        public CatchUpSubscriptionFilteredSettings CatchUpSubscriptionFilteredSettings { get; set; } = CatchUpSubscriptionFilteredSettings.Default;
        public bool IsSubscribeAll => false;
        public bool UseSnapshot { get; set; }
    }
}
