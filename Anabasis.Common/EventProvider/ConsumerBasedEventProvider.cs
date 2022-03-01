using System;
using System.Collections.Generic;
using System.Linq;

namespace Anabasis.Common
{

    public class ConsumerBasedEventProvider : IEventTypeProvider
    {
        private readonly Dictionary<string, Type> _eventTypeCache;

        public ConsumerBasedEventProvider(Type type)
        {
            _eventTypeCache = new Dictionary<string, Type>();

            var eventTypes = type.GetMethods().Where(method => method.Name == "Handle" && method.GetParameters().Length == 1)
              .Select(method =>
              {
                  var parameterType = method.GetParameters().First().ParameterType;

                  if (!parameterType.GetInterfaces().Contains(typeof(IEvent))) return null;

                  return parameterType;

              }).Where(type => null != type).ToArray();

            foreach (var @event in eventTypes)
            {
                _eventTypeCache.Add(@event.FullName, @event);
            }
        }

        public Type[] GetAll()
        {
            return _eventTypeCache.Values.ToArray();
        }

        public Type GetEventTypeByName(string name)
        {

            if (!_eventTypeCache.ContainsKey(name)) return null;

            return _eventTypeCache[name];

        }
    }

    public class ConsumerBasedEventProvider<TConsumer> : ConsumerBasedEventProvider
    {
        public ConsumerBasedEventProvider() : base(typeof(TConsumer))
        {
        }

    }

    public class ConsumerBasedEventProvider<TAggregate, TConsumer> : ConsumerBasedEventProvider<TConsumer>, IEventTypeProvider<TAggregate> where TAggregate : IAggregate, new()
    {
        public ConsumerBasedEventProvider()
        {
        }
    }
}

