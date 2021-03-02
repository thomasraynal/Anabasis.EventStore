using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
  public interface IEntityEvent<TKey> 
  {
    string Name { get; set; }
    TKey EntityId { get; set; }
    string GetStreamName();
  }
}
