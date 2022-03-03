using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.EventStore.Actor;
using System;

namespace Anabasis.EventStore.Factories
{
    public interface IEventStoreActorConfigurationFactory: IActorConfigurationFactory
    {
        void AddConfiguration<TActor, TAggregate>(IEventStoreActorConfiguration<TAggregate> actorConfiguration) 
            where TActor : IStatefulActor<TAggregate> 
            where TAggregate : IAggregate, new();

        IEventStoreActorConfiguration<TAggregate> GetConfiguration<TAggregate>(Type type) where TAggregate : IAggregate, new();
    }
}
