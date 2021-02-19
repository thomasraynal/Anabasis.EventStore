using Anabasis.Common;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Anabasis.EventStore.Infrastructure
{

  public class ServiceCollectionEventTypeProvider<TKey> : IEventTypeProvider<TKey>
  {
    private readonly Dictionary<string, Type> _eventTypeCache;
    private readonly IServiceProvider _serviceProvider;

    public ServiceCollectionEventTypeProvider(IServiceProvider serviceProvider)
    {
      _eventTypeCache = new Dictionary<string, Type>();
      _serviceProvider = serviceProvider;
    }

    //refacto
    public Type[] GetAll()
    {
      return _serviceProvider.GetServices<IEvent<TKey>>().Select(type => type.GetType()).ToArray();
    }

    public Type GetEventTypeByName(string name)
    {
      return _eventTypeCache.GetOrAdd(name, (key) =>
      {
        var type = _serviceProvider.GetServices<IEvent<TKey>>()
                               .FirstOrDefault(type => type.GetType().FullName == name);

        if (null == type) throw new InvalidOperationException($"Event {name} is not registered");

        return type.GetType();

      });
    }
  }

  public class ServiceCollectionEventTypeProvider<TKey, TCacheItem> : IEventTypeProvider<TKey, TCacheItem> where TCacheItem : IAggregate<TKey>
  {
    private readonly Dictionary<string, Type> _eventTypeCache;
    private readonly IServiceProvider _serviceProvider;

    public ServiceCollectionEventTypeProvider(IServiceProvider serviceProvider)
    {
      _eventTypeCache = new Dictionary<string, Type>();
      _serviceProvider = serviceProvider;
    }
    //refacto
    public Type[] GetAll()
    {
      return _serviceProvider.GetServices<IMutable<TKey, TCacheItem>>().Select(type => type.GetType()).ToArray();
    }

    public Type GetEventTypeByName(string name)
    {
      return _eventTypeCache.GetOrAdd(name, (key) =>
      {
        var type = _serviceProvider.GetServices<IMutable<TKey, TCacheItem>>()
                               .FirstOrDefault(type => type.GetType().FullName == name);

        if (null == type) throw new InvalidOperationException($"Event {name} is not registered");

        return type.GetType();

      });
    }
  }
}
