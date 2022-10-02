using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Configuration
{
    public class ActorConfigurationFactory : IActorConfigurationFactory
    {
        private readonly Dictionary<Type, IEventTypeProvider> _actorEventTypeProviders;
        private readonly Dictionary<Type, IActorConfiguration> _actorConfigurations;
        private readonly Dictionary<Type, IAggregateCacheConfiguration> _actorAggregateCacheConfigurations;

        public ActorConfigurationFactory()
        {
            _actorEventTypeProviders = new Dictionary<Type, IEventTypeProvider>();
            _actorConfigurations = new Dictionary<Type, IActorConfiguration>();
            _actorAggregateCacheConfigurations = new Dictionary<Type, IAggregateCacheConfiguration>();
        }

        public void AddActorConfiguration<TActor>(IActorConfiguration actorConfiguration)
        {
            _actorConfigurations.Add(typeof(TActor), actorConfiguration);
        }

        public void AddAggregateCacheConfiguration<TActor, TAggregateCacheConfiguration, TAggregate>(TAggregateCacheConfiguration aggregateCacheConfiguration)
            where TActor : class, IStatefulActor<TAggregate, TAggregateCacheConfiguration>
            where TAggregateCacheConfiguration : IAggregateCacheConfiguration, new()
            where TAggregate : IAggregate, new()
        {
            _actorAggregateCacheConfigurations.Add(typeof(TActor), aggregateCacheConfiguration);
        }

        public void AddEventTypeProvider<TActor>(IEventTypeProvider eventTypeProvider)
        {
            _actorEventTypeProviders.Add(typeof(TActor), eventTypeProvider);
        }

        public IActorConfiguration GetActorConfiguration(Type type)
        {
            if (!_actorConfigurations.ContainsKey(type))
                throw new InvalidOperationException($"Unable to find a configuration {typeof(IActorConfiguration)}  for actor {type}");

            return _actorConfigurations[type];
        }

        public TAggregateCacheConfiguration GetAggregateCacheConfiguration<TAggregateCacheConfiguration>(Type type) where TAggregateCacheConfiguration : IAggregateCacheConfiguration
        {
            if (!_actorAggregateCacheConfigurations.ContainsKey(type))
                throw new InvalidOperationException($"Unable to find configuration {typeof(TAggregateCacheConfiguration)} for actor {type}");

            return (TAggregateCacheConfiguration)_actorAggregateCacheConfigurations[type];
        }

        public IEventTypeProvider GetEventTypeProvider(Type type)
        {
            {
                if (!_actorEventTypeProviders.ContainsKey(type))
                    throw new InvalidOperationException($"Unable to find a configuration {typeof(IEventTypeProvider)} for actor {type}");

                return _actorEventTypeProviders[type];
            }
        }
    }
}
