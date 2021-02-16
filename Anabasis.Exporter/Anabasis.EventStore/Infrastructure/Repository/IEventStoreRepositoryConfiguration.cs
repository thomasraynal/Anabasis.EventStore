using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
    public interface IEventStoreRepositoryConfiguration<TKey>
    {
        int WritePageSize { get; set; }
        int ReadPageSize { get; set; }
        ISerializer Serializer { get; set; }
    }
}
