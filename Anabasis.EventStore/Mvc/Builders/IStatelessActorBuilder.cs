using Anabasis.Common;
using Anabasis.EventStore.Connection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Anabasis.EventStore
{
    public interface IStatelessActorBuilder
    {
        Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreStream>[] GetStreamFactories();
    }
}