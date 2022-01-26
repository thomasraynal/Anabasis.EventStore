using Anabasis.Common;
using System;

namespace Anabasis.EventStore.Mvc
{
    public interface IActorConfigurationFactory
    {
        void Add<TActor>(IActorConfiguration actorConfiguration);
        IActorConfiguration Get(Type actorType);
    }
}