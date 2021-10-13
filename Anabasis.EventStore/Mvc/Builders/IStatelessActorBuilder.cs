using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Queue;
using System;

namespace Anabasis.EventStore
{
    public interface IStatelessActorBuilder
    {
        Func<IConnectionStatusMonitor, IEventStoreQueue>[] GetQueueFactories();
    }
}