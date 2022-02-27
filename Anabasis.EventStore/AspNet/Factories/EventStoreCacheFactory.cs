using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.EventStore.Actor;
using System;
using System.Collections.Generic;

namespace Anabasis.EventStore.AspNet.Factories
{
    public class EventStoreCacheFactory : ActorConfigurationFactory, IEventStoreActorConfigurationFactory
    {
        private readonly Dictionary<Type, object> _eventStoreCaches;

        public EventStoreCacheFactory()
        {
            _eventStoreCaches = new Dictionary<Type, object>();
        }

        public void AddConfiguration<TActor, TAggregate>(IEventStoreActorConfiguration<TAggregate> eventStoreActorConfiguration)
            where TActor : IEventStoreStatefulActor<TAggregate>
            where TAggregate : IAggregate, new()
        {
            _eventStoreCaches.Add(typeof(TActor), eventStoreActorConfiguration);
        }

        public IEventStoreActorConfiguration<TAggregate> GetConfiguration<TAggregate>(Type type) where TAggregate : IAggregate, new()
        {
            if (!_eventStoreCaches.ContainsKey(type))
                throw new InvalidOperationException($"Unable to find a configuration for actor {type}");

            return (IEventStoreActorConfiguration<TAggregate>)_eventStoreCaches[type];
        }

    }
}
