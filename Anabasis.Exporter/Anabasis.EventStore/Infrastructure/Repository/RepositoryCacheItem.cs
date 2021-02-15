using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
    public class RepositoryCacheItem<TKey>
    {
        public KeyValuePair<string, string>[] Headers { get; set; }
        public IAggregate<TKey> Aggregate { get; set; }
    }
}
