using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.Common.Configuration
{
    public interface IActorConfigurationFactory
    {
        void AddConfiguration<TActor>(IActorConfiguration actorConfiguration);
        IActorConfiguration GetConfiguration(Type type);
    }
}
