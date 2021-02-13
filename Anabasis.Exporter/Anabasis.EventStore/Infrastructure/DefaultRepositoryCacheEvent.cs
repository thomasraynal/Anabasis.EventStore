using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
    public class DefaultRepositoryEventCache<TKey> : IRepositoryEventCache<TKey>
    {
        private ConcurrentStack<RepositoryCacheItem<TKey>> _cacheItems;

        public int Count => _cacheItems.Count;

        public DefaultRepositoryEventCache()
        {
            _cacheItems = new ConcurrentStack<RepositoryCacheItem<TKey>>();
        }

        public bool TryPop(out RepositoryCacheItem<TKey> item)
        {
            return !_cacheItems.TryPop(out item);
        }

        public void Push(RepositoryCacheItem<TKey> item)
        {
            _cacheItems.Push(item);
        }
    }
}
