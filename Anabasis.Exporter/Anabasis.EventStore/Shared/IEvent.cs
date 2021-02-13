using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
    public interface IEvent<TKey>: IEventStoreEntity<TKey>
    {
        String Name { get; set; }
    }
}
