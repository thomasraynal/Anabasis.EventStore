using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Anabasis.EventStore
{
    public class DefaultEventCache<TKey, TCacheItem> : EventStoreCache<TKey, TCacheItem>
        where TCacheItem : IAggregate<TKey>, new()
    {
        public DefaultEventCache(IConnectionStatusMonitor connectionMonitor,
          Type[] eventTypes,
          IEventStoreCacheConfiguration<TKey, TCacheItem> cacheConfiguration,
          IEventStoreRepositoryConfiguration<TKey> repositoryConfiguration,
          ILogger<EventStoreCache<TKey, TCacheItem>> logger = null)
            : base(connectionMonitor, cacheConfiguration, eventTypes, repositoryConfiguration, logger)
        {
        }

    }
}
