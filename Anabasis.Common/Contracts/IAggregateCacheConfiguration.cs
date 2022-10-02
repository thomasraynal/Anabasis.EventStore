using System;

namespace Anabasis.Common
{
    public interface IAggregateCacheConfiguration
    {
        TimeSpan IsStaleTimeSpan { get; }
        bool KeepAppliedEventsOnAggregate { get; }
        ISerializer Serializer { get; }
        public bool CrashAppIfSubscriptionFail { get; set; }
        public bool UseSnapshot { get; set; }
    }
}
