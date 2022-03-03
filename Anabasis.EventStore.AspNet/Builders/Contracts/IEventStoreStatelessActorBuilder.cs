using Anabasis.Common;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;

namespace Anabasis.EventStore.AspNet.Builders
{
    public interface IEventStoreStatelessActorBuilder: IActorBuilder
    {
        Func<IConnectionStatusMonitor<IEventStoreConnection>, ILoggerFactory, IEventStoreStream>[] GetStreamFactories();
    }
}