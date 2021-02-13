using EventStore.ClientAPI;
using EventStore.Core;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Anabasis.EventStore
{
  public static class ServiceBuilderExtensions
  {
    public static IServiceCollection AddEventStoreCache<TKey, TCacheItem, TEventStoreCache>(this IServiceCollection services, Action<IEventStoreCacheConfiguration<TKey, TCacheItem>> cacheBuilder = null)
        where TEventStoreCache : class, IEventStoreCache<TKey, TCacheItem>
        where TCacheItem : IAggregate<TKey>
    {
      RegisterCacheConfiguration(services, cacheBuilder);
      return services.AddSingleton<IEventStoreCache<TKey, TCacheItem>, TEventStoreCache>();
    }

    public static IServiceCollection AddEventStoreCache<TKey, TCacheItem>(this IServiceCollection services, Action<IEventStoreCacheConfiguration<TKey, TCacheItem>> cacheBuilder = null) where TCacheItem : IAggregate<TKey>, new()
    {
      RegisterCacheConfiguration(services, cacheBuilder);
      return services.AddSingleton<IEventStoreCache<TKey, TCacheItem>, DefaultEventCache<TKey, TCacheItem>>();
    }

    private static void RegisterCacheConfiguration<TKey, TCacheItem>(IServiceCollection services, Action<IEventStoreCacheConfiguration<TKey, TCacheItem>> cacheBuilder) where TCacheItem : IAggregate<TKey>
    {
      var configuration = new EventStoreCacheConfiguration<TKey, TCacheItem>();
      cacheBuilder?.Invoke(configuration);
      services.AddSingleton<IEventStoreCacheConfiguration<TKey, TCacheItem>>(configuration);
    }

    public static IServiceCollection AddEventStore<TKey, TRepository>(this IServiceCollection services, string eventStoreUrl, ConnectionSettings settings, Action<IEventStoreRepositoryConfiguration<TKey>> repositoryBuilder = null) where TRepository : class, IEventStoreRepository<TKey>
    {
      var eventStoreConnection = new ExternalEventStore(eventStoreUrl, settings).Connection;

      return AddEventStoreInternal<TKey, TRepository>(eventStoreConnection, services, repositoryBuilder);
    }


    public static IServiceCollection AddEventStore<TKey, TRepository>(this IServiceCollection services, ClusterVNode  clusterVNode, ConnectionSettings settings, Action<IEventStoreRepositoryConfiguration<TKey>> repositoryBuilder = null) where TRepository : class, IEventStoreRepository<TKey>
    {
      var eventStoreConnection = new ExternalEventStore(clusterVNode, settings).Connection;

      return AddEventStoreInternal<TKey, TRepository>(eventStoreConnection, services, repositoryBuilder);
    }

    public static IServiceCollection AddEventStore<TKey, TRepository>(this IServiceCollection services, string eventStoreUrl, Action<IEventStoreRepositoryConfiguration<TKey>> repositoryBuilder = null) where TRepository : class, IEventStoreRepository<TKey>
    {
      var eventStoreConnection = new ExternalEventStore(eventStoreUrl).Connection;

      return AddEventStoreInternal<TKey, TRepository>(eventStoreConnection, services, repositoryBuilder);
    }

    public static IServiceCollection AddEventStore<TKey, TRepository>(this IServiceCollection services, IEventStoreConnection eventStoreConnection) where TRepository : class, IEventStoreRepository<TKey>
    {
      return AddEventStoreInternal<TKey, TRepository>(eventStoreConnection, services);
    }

    private static IServiceCollection AddEventStoreInternal<TKey, TRepository>(IEventStoreConnection eventStoreConnection, IServiceCollection services, Action<IEventStoreRepositoryConfiguration<TKey>> repositoryBuilder = null) where TRepository : class, IEventStoreRepository<TKey>
    {
      return RegisterEventStoreRepository(eventStoreConnection, services, repositoryBuilder);
    }

    private static IServiceCollection RegisterEventStoreRepository<TKey>(IEventStoreConnection eventStoreConnection, IServiceCollection services, Action<IEventStoreRepositoryConfiguration<TKey>> repositoryBuilder = null)
    {
      services.AddSingleton(eventStoreConnection);
      services.AddSingleton<IConnectionStatusMonitor, ConnectionStatusMonitor>();
      services.AddSingleton<IEventStoreRepository<TKey>, EventStoreRepository<TKey>>();

      var configuration = new EventStoreRepositoryConfiguration<TKey>();

      repositoryBuilder?.Invoke(configuration);

      services.AddSingleton<IEventStoreRepositoryConfiguration<TKey>>(configuration);

      return services;

    }
  }
}
