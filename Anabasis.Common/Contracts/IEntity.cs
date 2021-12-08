using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{
  public interface IEntity<TKey> : IHaveAStreamId
  {
    TKey EntityId { get; set; } 
  }
}
