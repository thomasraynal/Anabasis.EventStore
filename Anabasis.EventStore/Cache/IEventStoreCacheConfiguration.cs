using Anabasis.Common;
using Anabasis.EventStore.Shared;
using EventStore.ClientAPI.SystemData;
using System;

namespace Anabasis.EventStore.Cache
{
    public interface IEventStoreCacheConfiguration<TAggregate> where TAggregate : IAggregate
    {
        TimeSpan IsStaleTimeSpan { get; }
        bool KeepAppliedEventsOnAggregate { get; }
        UserCredentials UserCredentials { get; }
        ISerializer Serializer { get; }
        public bool DoAppCrashIfSubscriptionFail { get; set; }
    }
}
