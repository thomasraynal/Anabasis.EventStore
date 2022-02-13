using Anabasis.Common;
using Anabasis.Common.Utilities;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Anabasis.EventStore.EventProvider
{

  public class ServiceCollectionEventTypeProvider : IEventTypeProvider
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
      return _serviceProvider.GetServices<IEntity>().Select(type => type.GetType()).ToArray();
    }

    public Type GetEventTypeByName(string name)
    {
      return _eventTypeCache.GetOrAdd(name, (key) =>
      {
        var type = _serviceProvider.GetServices<IEntity>()
                               .FirstOrDefault(type => type.GetType().FullName == name);

        if (null == type) return null;

        return type.GetType();

      });
    }
  }

  public class ServiceCollectionEventTypeProvider<TAggregate> : IEventTypeProvider<TAggregate> where TAggregate : IAggregate
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
      return _serviceProvider.GetServices<IMutation< TAggregate>>().Select(type => type.GetType()).ToArray();
    }

    public Type GetEventTypeByName(string name)
    {
      return _eventTypeCache.GetOrAdd(name, (key) =>
      {
        var type = _serviceProvider.GetServices<IMutation< TAggregate>>()
                               .FirstOrDefault(type => type.GetType().FullName == name);

        if (null == type) return null;

        return type.GetType();

      });
    }
  }
}
