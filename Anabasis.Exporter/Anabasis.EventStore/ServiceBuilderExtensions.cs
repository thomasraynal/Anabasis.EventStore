using Anabasis.EventStore.Infrastructure;
using Anabasis.EventStore.Infrastructure.Cache.CatchupSubscription;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Core;
using Microsoft.Extensions.DependencyInjection;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using System;

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
