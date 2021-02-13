using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
    public interface IEventStoreEntity<TKey>
    {
        TKey EntityId { get; set; }
        string ToStreamId();
    }
}
