using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Mvc
{
    public class ActorConfigurationFactory : IActorConfigurationFactory
    {
        private readonly Dictionary<Type, IActorConfiguration> _actorConfigurations;

        public ActorConfigurationFactory()
        {
            _actorConfigurations = new Dictionary<Type, IActorConfiguration>();
        }

        public IActorConfiguration Get(Type actorType)
        {
            return _actorConfigurations[actorType];
        }

        public void Add<TActor>(IActorConfiguration actorConfiguration)
        {
            _actorConfigurations.Add(typeof(TActor), actorConfiguration);
        }
    }
}
