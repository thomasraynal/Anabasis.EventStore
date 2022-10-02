using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Configuration
{
    public interface IActorConfigurationFactory
    {
        void AddAggregateCacheConfiguration<TActor, TAggregateCacheConfiguration, TAggregate>(TAggregateCacheConfiguration aggregateCacheConfiguration)
            where TActor : class, IStatefulActor<TAggregate, TAggregateCacheConfiguration>
            where TAggregateCacheConfiguration : IAggregateCacheConfiguration, new()
            where TAggregate : IAggregate, new();
        void AddActorConfiguration<TActor>(IActorConfiguration actorConfiguration);
        void AddEventTypeProvider<TActor>(IEventTypeProvider eventTypeProvider);
        IActorConfiguration GetActorConfiguration(Type type);
        IEventTypeProvider GetEventTypeProvider(Type type);
        TAggregateCacheConfiguration GetAggregateCacheConfiguration<TAggregateCacheConfiguration>(Type type) where TAggregateCacheConfiguration : IAggregateCacheConfiguration;
    }
}
