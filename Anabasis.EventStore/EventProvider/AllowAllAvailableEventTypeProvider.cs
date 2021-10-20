using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Anabasis.EventStore.EventProvider
{
    public class AllowAllAvailableEventTypeProvider : IEventTypeProvider
    {
        public AllowAllAvailableEventTypeProvider()
        {
        }

        public Type[] GetAll()
        {
            return new Type[0];
        }

        public Type GetEventTypeByName(string name)
        {
            throw new NotImplementedException();
        }
    }
}
