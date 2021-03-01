using Anabasis.EventStore.Infrastructure;
using Anabasis.EventStore.Infrastructure.Cache.CatchupSubscription;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Core;
using Microsoft.Extensions.DependencyInjection;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using System;
using Anabasis.EventStore.Infrastructure.Cache.VolatileSubscription;
using Anabasis.EventStore.Infrastructure.Cache;

namespace Anabasis.EventStore
{
  public static class ServiceBuilderExtensions
  {
    public static IServiceCollection AddEventStoreCatchupCache<TKey, TAggregate>(this IServiceCollection services,
      ClusterVNode clusterVNode,
      UserCredentials userCredentials,
      ConnectionSettings connectionSettings,
      Action<CatchupEventStoreCacheConfiguration<TKey, TAggregate>> cacheBuilder = null)
        where TAggregate : IAggregate<TKey>, new()
    {

      var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);
      var connectionMonitor = new ConnectionStatusMonitor(connection);

      var catchupEventStoreCacheConfiguration = new CatchupEventStoreCacheConfiguration<TKey, TAggregate>(userCredentials);
      cacheBuilder?.Invoke(catchupEventStoreCacheConfiguration);

      services.AddTransient<IEventTypeProvider<TKey, TAggregate>, ServiceCollectionEventTypeProvider<TKey, TAggregate>>();
      services.AddSingleton(catchupEventStoreCacheConfiguration);
      services.AddSingleton<IConnectionStatusMonitor>(connectionMonitor);
      services.AddTransient<CatchupEventStoreCache<TKey, TAggregate>>();

      return services;

    }

    public static IServiceCollection AddEventStoreSingleStreamCatchupCache<TKey, TAggregate>(this IServiceCollection services,
      ClusterVNode clusterVNode,
      UserCredentials userCredentials,
      ConnectionSettings connectionSettings,
      string streamId,
      Action<SingleStreamCatchupEventStoreCacheConfiguration<TKey, TAggregate>> cacheBuilder = null)
    where TAggregate : IAggregate<TKey>, new()
    {

      var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);
      var connectionMonitor = new ConnectionStatusMonitor(connection);

      var singleStreamCatchupEventStoreCacheConfiguration = new SingleStreamCatchupEventStoreCacheConfiguration<TKey, TAggregate>(streamId, userCredentials);
      cacheBuilder?.Invoke(singleStreamCatchupEventStoreCacheConfiguration);

      services.AddTransient<IEventTypeProvider<TKey, TAggregate>, ServiceCollectionEventTypeProvider<TKey, TAggregate>>();
      services.AddSingleton(singleStreamCatchupEventStoreCacheConfiguration);
      services.AddSingleton<IConnectionStatusMonitor>(connectionMonitor);
      services.AddTransient<SingleStreamCatchupEventStoreCache<TKey, TAggregate>>();

      return services;

    }

    public static IServiceCollection AddEventStoreVolatileCache<TKey, TAggregate>(this IServiceCollection services,
      ClusterVNode clusterVNode,
      UserCredentials userCredentials,
      ConnectionSettings connectionSettings,
      Action<VolatileCacheConfiguration<TKey, TAggregate>> cacheBuilder = null)
    where TAggregate : IAggregate<TKey>, new()
    {

      var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);
      var connectionMonitor = new ConnectionStatusMonitor(connection);

      var volatileCacheConfiguration = new VolatileCacheConfiguration<TKey, TAggregate>(userCredentials);
      cacheBuilder?.Invoke(volatileCacheConfiguration);

      services.AddTransient<IEventTypeProvider<TKey, TAggregate>, ServiceCollectionEventTypeProvider<TKey, TAggregate>>();
      services.AddSingleton(volatileCacheConfiguration);
      services.AddSingleton<IConnectionStatusMonitor>(connectionMonitor);
      services.AddTransient<VolatileEventStoreCache<TKey, TAggregate>>();

      return services;

    }

    public static IServiceCollection AddEventStorePersistentSubscriptionCache<TKey, TAggregate>(this IServiceCollection services,
    ClusterVNode clusterVNode,
    UserCredentials userCredentials,
    ConnectionSettings connectionSettings,
    string streamId,
    string groupId,
    Action<PersistentSubscriptionCacheConfiguration<TKey, TAggregate>> cacheBuilder = null)
  where TAggregate : IAggregate<TKey>, new()
    {

      var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);
      var connectionMonitor = new ConnectionStatusMonitor(connection);

      var persistentSubscriptionCacheConfiguration = new PersistentSubscriptionCacheConfiguration<TKey, TAggregate>(streamId, groupId, userCredentials);
      cacheBuilder?.Invoke(persistentSubscriptionCacheConfiguration);

      services.AddTransient<IEventTypeProvider<TKey, TAggregate>, ServiceCollectionEventTypeProvider<TKey, TAggregate>>();
      services.AddSingleton(persistentSubscriptionCacheConfiguration);
      services.AddSingleton<IConnectionStatusMonitor>(connectionMonitor);
      services.AddTransient<PersistentSubscriptionEventStoreCache<TKey, TAggregate>>();

      return services;

    }

    #region Repository

    public static IServiceCollection AddEventStoreRepository<TKey, TRepository>(this IServiceCollection services,
      ClusterVNode clusterVNode,
      ConnectionSettings connectionSettings,
      UserCredentials userCredentials,
      Action<IEventStoreRepositoryConfiguration<TKey>> repositoryBuilder = null) where TRepository : class, IEventStoreRepository<TKey>
    {
      var eventStoreConnection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);

      services.AddSingleton(eventStoreConnection);

      services.AddSingleton<IConnectionStatusMonitor, ConnectionStatusMonitor>();
      services.AddSingleton<IEventStoreRepository<TKey>, EventStoreRepository<TKey>>();
      services.AddTransient<IEventTypeProvider<TKey>, ServiceCollectionEventTypeProvider<TKey>>();

      var configuration = new EventStoreRepositoryConfiguration<TKey>(userCredentials, connectionSettings);

      repositoryBuilder?.Invoke(configuration);

      services.AddSingleton<IEventStoreRepositoryConfiguration<TKey>>(configuration);

      return services;
    }



    #endregion repositiory

  }
}
