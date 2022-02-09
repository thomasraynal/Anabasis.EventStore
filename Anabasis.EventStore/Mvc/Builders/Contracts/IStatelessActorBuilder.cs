using Anabasis.Common;
using System;

namespace Anabasis.EventStore.Mvc
{
    public interface IStatelessActorBuilder
    {
        (Type actor, Action<IServiceProvider, IActor> factory)[] GetBusFactories();
    }
}