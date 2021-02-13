using Anabasis.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Anabasis.EventStore.Infrastructure
{
  public class DefaultEventTypeProvider<TKey> : IEventTypeProvider<TKey>
  {
    private readonly Dictionary<string, Type> _eventTypeCache;
    private readonly Func<string, Type> _serviceProvider;

    public DefaultEventTypeProvider(Func<string,Type> eventTypeProvider)
    {
      _eventTypeCache = new Dictionary<string, Type>();
      _serviceProvider = eventTypeProvider;
    }

    public Type GetEventTypeByName(string name)
    {
      return _eventTypeCache.GetOrAdd(name, (key) =>
      {
        var type = _serviceProvider(name);

        if (null == type) throw new InvalidOperationException($"Event {name} is not registered");

        return type.GetType();

      });
    }
  }

  public class DefaultEventTypeProvider<TKey, TCacheItem> : IEventTypeProvider<TKey, TCacheItem> where TCacheItem : IAggregate<TKey>
  {
    private readonly Dictionary<string, Type> _eventTypeCache;
    private readonly Func<string, Type> _serviceProvider;

    public DefaultEventTypeProvider(Func<string, Type> eventTypeProvider)
    {
      _eventTypeCache = new Dictionary<string, Type>();
      _serviceProvider = eventTypeProvider;
    }

    public Type GetEventTypeByName(string name)
    {
      return _eventTypeCache.GetOrAdd(name, (key) =>
      {
        var type = _serviceProvider(name);

        if (null == type) throw new InvalidOperationException($"Event {name} is not registered");

        return type.GetType();

      });
    }
  }
}

