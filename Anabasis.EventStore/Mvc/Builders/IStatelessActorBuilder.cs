using Anabasis.Common;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Stream;
using Microsoft.Extensions.Logging;
using System;

namespace Anabasis.EventStore
{
    public interface IStatelessActorBuilder
    {
        Func<IConnectionStatusMonitor, ILoggerFactory, IEventStream>[] GetStreamFactories();
    }
}