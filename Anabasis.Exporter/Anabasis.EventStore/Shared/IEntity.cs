using Anabasis.EventStore.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore
{
  public interface IEntity<TKey> : IHaveAStreamId
  {
    TKey EntityId { get; set; } 
  }
}
