using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
    public interface IEventStoreCacheConfiguration<TKey,TCacheItem> where TCacheItem : IAggregate<TKey>
    {
        bool AddAppliedEventsOnAggregate { get; set; }
    }
}
