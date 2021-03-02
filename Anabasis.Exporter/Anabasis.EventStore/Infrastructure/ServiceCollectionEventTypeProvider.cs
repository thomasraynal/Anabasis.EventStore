using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

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
      return _serviceProvider.GetServices<IEntityEvent<TKey>>().Select(type => type.GetType()).ToArray();
    }

    public Type GetEventTypeByName(string name)
    {
      return _eventTypeCache.GetOrAdd(name, (key) =>
      {
        var type = _serviceProvider.GetServices<IEntityEvent<TKey>>()
                               .FirstOrDefault(type => type.GetType().FullName == name);

        if (null == type) throw new InvalidOperationException($"Event {name} is not registered");

        return type.GetType();

      });
    }
  }

  public class ServiceCollectionEventTypeProvider<TKey, TAggregate> : IEventTypeProvider<TKey, TAggregate> where TAggregate : IAggregate<TKey>
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
      return _serviceProvider.GetServices<IMutable<TKey, TAggregate>>().Select(type => type.GetType()).ToArray();
    }

    public Type GetEventTypeByName(string name)
    {
      return _eventTypeCache.GetOrAdd(name, (key) =>
      {
        var type = _serviceProvider.GetServices<IMutable<TKey, TAggregate>>()
                               .FirstOrDefault(type => type.GetType().FullName == name);

        if (null == type) throw new InvalidOperationException($"Event {name} is not registered");

        return type.GetType();

      });
    }
  }
}
