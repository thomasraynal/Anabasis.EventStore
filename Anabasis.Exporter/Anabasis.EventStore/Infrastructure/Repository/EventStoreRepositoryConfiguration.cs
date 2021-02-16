using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
    public class EventStoreRepositoryConfiguration<TKey> : IEventStoreRepositoryConfiguration<TKey>
    {
        public int WritePageSize { get; set; } = 500;
        public int ReadPageSize { get; set; } = 500;
        public ISerializer Serializer { get; set; } = new DefaultSerializer();
    }
}
