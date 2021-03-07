using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Infrastructure
{
  public interface IEventTypeProvider
  {
    Type[] GetAll();
    Type GetEventTypeByName(string name);
  }
}
