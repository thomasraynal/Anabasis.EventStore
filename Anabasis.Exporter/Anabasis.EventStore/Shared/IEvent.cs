using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
    public interface IEvent<TKey>: IEventStoreEntity<TKey>
    {
    //Guid EventId { get; set; }
    String Name { get; set; }
    }
}
