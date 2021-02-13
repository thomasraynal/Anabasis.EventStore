using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
    public class EventStoreCacheConfiguration<TKey, TCacheItem> : IEventStoreCacheConfiguration<TKey, TCacheItem> where TCacheItem : IAggregate<TKey>
    {
        public bool AddAppliedEventsOnAggregate { get; set; }
    }
}
