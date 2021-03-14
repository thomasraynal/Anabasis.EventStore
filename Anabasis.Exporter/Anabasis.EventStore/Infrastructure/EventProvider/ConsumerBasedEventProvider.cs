using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Anabasis.EventStore.Infrastructure
{
  public class ConsumerBasedEventProvider<TConsumer> : IEventTypeProvider
  {
    private readonly Dictionary<string, Type> _eventTypeCache;

    public ConsumerBasedEventProvider()
    {
      _eventTypeCache = new Dictionary<string, Type>();


      var eventTypes = typeof(TConsumer).GetMethods().Where(method => method.Name == "Handle" && method.GetParameters().Length == 1)
        .Select(method =>
        {
          var parameterType = method.GetParameters().First().ParameterType;

          if (!parameterType.GetInterfaces().Contains(typeof(IEvent))) return null;

          return parameterType;

        }).Where(type=> null != type).ToArray();

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
}
