using Anabasis.EventStore.Connection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Mvc
{
    public interface IStatefulActorBuilder
    {
        Func<IConnectionStatusMonitor, IEventStoreQueue>[] GetQueueFactories();
    }
}
