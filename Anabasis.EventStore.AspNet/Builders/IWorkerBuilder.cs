using Anabasis.Common;
using Anabasis.Common.Contracts;
using System;

namespace Anabasis.EventStore.AspNet.Builders
{
    public interface IWorkerBuilder
    {
        (Type workerType, Action<IServiceProvider, IWorker> factory)[] GetBusFactories();
    }
}