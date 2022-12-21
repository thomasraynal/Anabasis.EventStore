using Anabasis.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.ProtoActor.Tests
{
    public class TestEventInterceptor
    {
        public ConcurrentDictionary<string, int> ConcurrentDictionary { get; }

        public TestEventInterceptor()
        {
            ConcurrentDictionary = new ConcurrentDictionary<string, int>();
        }

        public void AddEvent(string receiverId, IEvent @event)
        {
            ConcurrentDictionary.AddOrUpdate(receiverId, 1, (key, current) =>
            {
                return ++current;
            });
        }
    }
}
