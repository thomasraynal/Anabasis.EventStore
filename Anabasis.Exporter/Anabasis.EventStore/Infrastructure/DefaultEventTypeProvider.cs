using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Anabasis.EventStore.Infrastructure
{
  public class DefaultEventTypeProvider<TKey> : IEventTypeProvider<TKey>
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

    public Type GetEventTypeByName(string name)
    {

      if (!_eventTypeCache.ContainsKey(name))
      {
        throw new InvalidOperationException($"Event {name} is not registered");
      }

      return _eventTypeCache[name];

    }
  }

  public class DefaultEventTypeProvider<TKey, TCacheItem> : IEventTypeProvider<TKey, TCacheItem> where TCacheItem : IAggregate<TKey>
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

    public Type GetEventTypeByName(string name)
    {

      if (!_eventTypeCache.ContainsKey(name))
      {
        throw new InvalidOperationException($"Event {name} is not registered");
      }

      return _eventTypeCache[name];

    }
  }
}

