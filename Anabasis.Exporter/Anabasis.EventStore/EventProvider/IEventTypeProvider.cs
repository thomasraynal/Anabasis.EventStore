using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.EventProvider
{
  public interface IEventTypeProvider
  {
    Type[] GetAll();
    Type GetEventTypeByName(string name);
  }

  public interface IEventTypeProvider<TKey, TAggregate> : IEventTypeProvider
  {
  }
}
