using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Infrastructure
{
  public interface IEventTypeProvider<TKey, TCacheItem> where TCacheItem : IAggregate<TKey>
  {
    Type[] GetAll();
    Type GetEventTypeByName(string name);
  }

  public interface IEventTypeProvider<TKey>
  {
    Type[] GetAll();
    Type GetEventTypeByName(string name);
  }
}
