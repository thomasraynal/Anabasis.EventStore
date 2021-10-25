using Anabasis.EventStore.Serialization;
using Anabasis.EventStore.Shared;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Cache
{
    public class MultipleStreamsCatchupCacheConfiguration<TKey, TAggregate> : IEventStoreCacheConfiguration<TKey, TAggregate> where TAggregate : IAggregate<TKey>
    {
        public MultipleStreamsCatchupCacheConfiguration(params string[] streamIds)
        {
            StreamIds = streamIds;
        }

        public string[] StreamIds { get;  }
        public TimeSpan IsStaleTimeSpan { get; set; } = TimeSpan.FromHours(1);
        public bool KeepAppliedEventsOnAggregate { get; set; } = false;
        public UserCredentials UserCredentials { get; set; }
        public ISerializer Serializer { get; set; } = new DefaultSerializer();
        public bool UseSnapshot { get; set; }
        public CatchUpSubscriptionFilteredSettings CatchUpSubscriptionFilteredSettings { get; set; } = CatchUpSubscriptionFilteredSettings.Default;
        public bool IsSubscribeAll => false;

    }
}
