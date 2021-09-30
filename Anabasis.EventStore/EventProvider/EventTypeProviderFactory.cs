using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Anabasis.EventStore.EventProvider
{
    public class EventTypeProviderFactory
    {
        private readonly Dictionary<Type, IEventTypeProvider> _registry;

        public EventTypeProviderFactory()
        {
            _registry = new Dictionary<Type, IEventTypeProvider>();
        }

        public void AddEventTypeProvider<TActor>(IEventTypeProvider eventTypeProvider)
        {
            _registry.Add(typeof(TActor), eventTypeProvider);
        }

        public IEventTypeProvider GetEventTypeProvider<TActor>()
        {
            var provider = _registry.GetValueOrDefault(typeof(TActor));

            if (null != provider)
                return provider;

            return new ConsumerBasedEventProvider<TActor>();
        }

    }
}
