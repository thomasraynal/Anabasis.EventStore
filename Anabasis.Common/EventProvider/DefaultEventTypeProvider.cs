using System;
using System.Collections.Generic;
using System.Linq;

namespace Anabasis.Common
{
    public class DefaultEventTypeProvider : IEventTypeProvider
    {
        private readonly Dictionary<string, Type> _eventTypeCache;

        public DefaultEventTypeProvider(Func<Type[]> eventTypeProvider)
        {
            _eventTypeCache = new Dictionary<string, Type>();

            foreach (var @event in eventTypeProvider())
            {
                _eventTypeCache.Add(@event.FullName, @event);
            }

        }

        public Type[] GetAll()
        {
            return _eventTypeCache.Values.ToArray();
        }

        public bool CanHandle(IEvent @event)
        {
            return _eventTypeCache.Values.Any(value => value == @event.GetType());
        }

        public Type? GetEventTypeByName(string name)
        {

            if (!_eventTypeCache.ContainsKey(name)) return null;

            return _eventTypeCache[name];

        }
    }

    public class DefaultEventTypeProvider<TAggregate> : IEventTypeProvider where TAggregate : IAggregate, new()
    {
        private readonly Dictionary<string, Type> _eventTypeCache;

        public DefaultEventTypeProvider(Func<Type[]> eventTypeProvider)
        {
            _eventTypeCache = new Dictionary<string, Type>();

            foreach (var @event in eventTypeProvider())
            {
                _eventTypeCache.Add(@event.FullName, @event);
            }

        }

        public bool CanHandle(IEvent @event)
        {
            return _eventTypeCache.Values.Any(value => value == @event.GetType());
        }
        public Type[] GetAll()
        {
            return _eventTypeCache.Values.ToArray();
        }

        public Type? GetEventTypeByName(string name)
        {

            if (!_eventTypeCache.ContainsKey(name)) return null;

            return _eventTypeCache[name];

        }
    }
}

