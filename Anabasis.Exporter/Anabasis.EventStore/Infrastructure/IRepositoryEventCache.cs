using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
    public interface IRepositoryEventCache<TKey>
    {
        int Count { get; }
        void Push(RepositoryCacheItem<TKey> item);
        bool TryPop(out RepositoryCacheItem<TKey> item);
    }
}
