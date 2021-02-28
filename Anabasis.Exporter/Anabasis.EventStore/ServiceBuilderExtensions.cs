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
    public static IServiceCollection AddEventStoreCatchupCache<TKey, TCacheItem>(this IServiceCollection services,
      ClusterVNode clusterVNode,
      UserCredentials userCredentials,
      ConnectionSettings connectionSettings,
      Action<CatchupEventStoreCacheConfiguration<TKey, TCacheItem>> cacheBuilder = null)
        where TCacheItem : IAggregate<TKey>, new()
    {

      var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);
      var connectionMonitor = new ConnectionStatusMonitor(connection);

      var catchupEventStoreCacheConfiguration = new CatchupEventStoreCacheConfiguration<TKey, TCacheItem>(userCredentials);
      cacheBuilder?.Invoke(catchupEventStoreCacheConfiguration);

      services.AddTransient<IEventTypeProvider<TKey, TCacheItem>, ServiceCollectionEventTypeProvider<TKey, TCacheItem>>();
      services.AddSingleton(catchupEventStoreCacheConfiguration);
      services.AddSingleton<IConnectionStatusMonitor>(connectionMonitor);
      services.AddTransient<CatchupEventStoreCache<TKey, TCacheItem>>();

      return services;

    }

    public static IServiceCollection AddEventStoreSingleStreamCatchupCache<TKey, TCacheItem>(this IServiceCollection services,
      ClusterVNode clusterVNode,
      UserCredentials userCredentials,
      ConnectionSettings connectionSettings,
      string streamId,
      Action<SingleStreamCatchupEventStoreCacheConfiguration<TKey, TCacheItem>> cacheBuilder = null)
    where TCacheItem : IAggregate<TKey>, new()
    {

      var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);
      var connectionMonitor = new ConnectionStatusMonitor(connection);

      var singleStreamCatchupEventStoreCacheConfiguration = new SingleStreamCatchupEventStoreCacheConfiguration<TKey, TCacheItem>(streamId, userCredentials);
      cacheBuilder?.Invoke(singleStreamCatchupEventStoreCacheConfiguration);

      services.AddTransient<IEventTypeProvider<TKey, TCacheItem>, ServiceCollectionEventTypeProvider<TKey, TCacheItem>>();
      services.AddSingleton(singleStreamCatchupEventStoreCacheConfiguration);
      services.AddSingleton<IConnectionStatusMonitor>(connectionMonitor);
      services.AddTransient<SingleStreamCatchupEventStoreCache<TKey, TCacheItem>>();

      return services;

    }

    public static IServiceCollection AddEventStoreVolatileCache<TKey, TCacheItem>(this IServiceCollection services,
      ClusterVNode clusterVNode,
      UserCredentials userCredentials,
      ConnectionSettings connectionSettings,
      Action<VolatileCacheConfiguration<TKey, TCacheItem>> cacheBuilder = null)
    where TCacheItem : IAggregate<TKey>, new()
    {

      var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);
      var connectionMonitor = new ConnectionStatusMonitor(connection);

      var volatileCacheConfiguration = new VolatileCacheConfiguration<TKey, TCacheItem>(userCredentials);
      cacheBuilder?.Invoke(volatileCacheConfiguration);

      services.AddTransient<IEventTypeProvider<TKey, TCacheItem>, ServiceCollectionEventTypeProvider<TKey, TCacheItem>>();
      services.AddSingleton(volatileCacheConfiguration);
      services.AddSingleton<IConnectionStatusMonitor>(connectionMonitor);
      services.AddTransient<VolatileEventStoreCache<TKey, TCacheItem>>();

      return services;

    }

    public static IServiceCollection AddEventStorePersistentSubscriptionCache<TKey, TCacheItem>(this IServiceCollection services,
    ClusterVNode clusterVNode,
    UserCredentials userCredentials,
    ConnectionSettings connectionSettings,
    string streamId,
    string groupId,
    Action<PersistentSubscriptionCacheConfiguration<TKey, TCacheItem>> cacheBuilder = null)
  where TCacheItem : IAggregate<TKey>, new()
    {

      var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);
      var connectionMonitor = new ConnectionStatusMonitor(connection);

      var persistentSubscriptionCacheConfiguration = new PersistentSubscriptionCacheConfiguration<TKey, TCacheItem>(streamId, groupId, userCredentials);
      cacheBuilder?.Invoke(persistentSubscriptionCacheConfiguration);

      services.AddTransient<IEventTypeProvider<TKey, TCacheItem>, ServiceCollectionEventTypeProvider<TKey, TCacheItem>>();
      services.AddSingleton(persistentSubscriptionCacheConfiguration);
      services.AddSingleton<IConnectionStatusMonitor>(connectionMonitor);
      services.AddTransient<PersistentSubscriptionEventStoreCache<TKey, TCacheItem>>();

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
