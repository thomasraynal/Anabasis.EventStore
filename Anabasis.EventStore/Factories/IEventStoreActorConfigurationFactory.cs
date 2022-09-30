using Anabasis.Common;
using Anabasis.Common.Configuration;
using System;

namespace Anabasis.EventStore.Factories
{
    public interface IEventStoreActorConfigurationFactory: IActorConfigurationFactory
    {
        void AddConfiguration<TActor, TAggregateCacheConfiguration, TAggregate>(IEventStoreActorConfiguration<TAggregate> actorConfiguration) 
            where TActor : IStatefulActor<TAggregate, TAggregateCacheConfiguration>
            where TAggregateCacheConfiguration : IAggregateCacheConfiguration<TAggregate>
            where TAggregate : IAggregate, new();

        IEventStoreActorConfiguration<TAggregate> GetConfiguration<TAggregate>(Type type) where TAggregate : IAggregate, new();
    }
}
