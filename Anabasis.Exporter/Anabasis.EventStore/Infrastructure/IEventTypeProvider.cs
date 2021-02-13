using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Infrastructure
{
  public interface IEventTypeProvider<TKey, TCacheItem> where TCacheItem : IAggregate<TKey>
  {
    Type GetEventTypeByName(string name);
  }

  public interface IEventTypeProvider<TKey>
  {
    Type GetEventTypeByName(string name);
  }
}
