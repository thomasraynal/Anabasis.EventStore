using Anabasis.Common;
using System;

namespace Anabasis.EventStore.AspNet.Builders
{
    public interface IStatelessActorBuilder
    {
        (Type actor, Action<IServiceProvider, IActor> factory)[] GetBusFactories();
    }
}