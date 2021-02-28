using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
  public interface IEvent<TKey> : IEventStoreEntity<TKey>
  {
    string Name { get; set; }
  }
}
