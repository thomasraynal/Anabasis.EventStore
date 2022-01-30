using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Configuration
{
    public class ActorConfigurationFactory : IActorConfigurationFactory
    {
        private readonly Dictionary<Type, IActorConfiguration> _actorConfigurations;

        public ActorConfigurationFactory()
        {
            _actorConfigurations = new Dictionary<Type, IActorConfiguration>();
        }

        public void AddConfiguration<TActor>(IActorConfiguration actorConfiguration)
        {
            _actorConfigurations.Add(typeof(TActor), actorConfiguration);
        }

        public IActorConfiguration GetConfiguration(Type type)
        {
            if (!_actorConfigurations.ContainsKey(type))
                throw new InvalidOperationException($"Unable to find a configuration for actor {type}");

            return _actorConfigurations[type];
        }
    }
}
