using Anabasis.Common;
using System;

namespace Anabasis.EventStore.AspNet.Builders
{
    public interface IActorBuilder
    {
        (Type actor, Action<IServiceProvider, IAnabasisActor> factory)[] GetBusFactories();
    }
}