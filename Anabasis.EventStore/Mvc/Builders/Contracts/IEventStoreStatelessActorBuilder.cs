using Anabasis.Common;
using Anabasis.EventStore.Connection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Mvc
{
    public interface IEventStoreStatelessActorBuilder: IStatelessActorBuilder
    {
        Func<IConnectionStatusMonitor, ILoggerFactory, IEventStoreStream>[] GetStreamFactories();
    }
}