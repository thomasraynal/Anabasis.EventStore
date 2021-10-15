using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Queue;
using Microsoft.Extensions.Logging;
using System;

namespace Anabasis.EventStore
{
    public interface IStatelessActorBuilder
    {
        Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreQueue>[] GetQueueFactories();
    }
}