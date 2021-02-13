using DynamicData;
using Anabasis.Tests.Demo;
using System;
using Anabasis.EventStore;

namespace Anabasis.Tests.Tests
{
    public class Consumer : IConsumer
    {
        private IEventStoreCache<Guid, Item> _cache;

        public Consumer(IEventStoreCache<Guid, Item> cache)
        {
            _cache = cache;
            OnChange = _cache.AsObservableCache();
        }

        public IObservableCache<Item, Guid> OnChange { get; }
    }
}
