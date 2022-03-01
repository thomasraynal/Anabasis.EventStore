using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common
{
    public interface IEventTypeProvider
    {
        Type[] GetAll();
        Type GetEventTypeByName(string name);
    }

    public interface IEventTypeProvider<TAggregate> : IEventTypeProvider
    {
    }
}
