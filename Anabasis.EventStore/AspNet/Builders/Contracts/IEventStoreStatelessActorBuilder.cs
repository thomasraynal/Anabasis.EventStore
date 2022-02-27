using Anabasis.Common;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;

namespace Anabasis.EventStore.AspNet.Builders
{
    public interface IEventStoreStatelessActorBuilder: IStatelessActorBuilder
    {
        Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IEventStoreStream>[] GetStreamFactories();
    }
}