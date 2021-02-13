using Anabasis.EventStore.Infrastructure;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Anabasis.EventStore
{

  public static class EventStoreBuild
  {

    public static EventStoreCache<TKey, TCacheItem> GetEventStoreCache<TKey, TCacheItem>(
      ClusterVNode clusterVNode,
      ConnectionSettings settings,
      IEventTypeProvider<TKey, TCacheItem> eventTypeProvider,
      Action<IEventStoreCacheConfiguration<TKey, TCacheItem>> eventStoreCacheConfigurationBuilder = null,
      ILogger logger = null) where TCacheItem : IAggregate<TKey>, new()
    {
      var eventStoreConnection = EmbeddedEventStoreConnection.Create(clusterVNode, settings);

      return GetEventStoreCacheInternal(eventStoreConnection, eventTypeProvider, eventStoreCacheConfigurationBuilder, logger);
    }

    public static EventStoreCache<TKey, TCacheItem> GetEventStoreCache<TKey, TCacheItem>(
      IEventStoreConnection eventStoreConnection,
      IEventTypeProvider<TKey, TCacheItem> eventTypeProvider,
      Action<IEventStoreCacheConfiguration<TKey, TCacheItem>> eventStoreCacheConfigurationBuilder = null,
      ILogger logger = null) where TCacheItem : IAggregate<TKey>, new()
    {
      return GetEventStoreCacheInternal(eventStoreConnection, eventTypeProvider, eventStoreCacheConfigurationBuilder, logger);
    }

    public static EventStoreCache<TKey, TCacheItem> GetEventStoreCache<TKey, TCacheItem>(
      string eventStoreUrl,
      ConnectionSettings settings,
      IEventTypeProvider<TKey, TCacheItem> eventTypeProvider,
      Action<IEventStoreCacheConfiguration<TKey, TCacheItem>> eventStoreCacheConfigurationBuilder = null,
      ILogger logger = null) where TCacheItem : IAggregate<TKey>, new()
    {
      var eventStoreConnection = EventStoreConnection.Create(settings, new Uri(eventStoreUrl));

      return GetEventStoreCacheInternal(eventStoreConnection, eventTypeProvider, eventStoreCacheConfigurationBuilder, logger);
    }

    public static EventStoreRepository<TKey> GetEventStoreRepository<TKey>(
      ClusterVNode clusterVNode,
      ConnectionSettings settings,
      IEventTypeProvider<TKey> eventTypeProvider,
      Action<IEventStoreRepositoryConfiguration<TKey>> eventStoreRepositoryConfigurationBuilder = null,
      ILogger logger = null)
    {
      var eventStoreConnection = EmbeddedEventStoreConnection.Create(clusterVNode, settings);

      return GetEventStoreRepositoryInternal(eventStoreConnection, eventTypeProvider, eventStoreRepositoryConfigurationBuilder, logger);
    }


    public static EventStoreRepository<TKey> GetEventStoreRepository<TKey>(
      IEventStoreConnection eventStoreConnection,
      IEventTypeProvider<TKey> eventTypeProvider,
      Action<IEventStoreRepositoryConfiguration<TKey>> eventStoreRepositoryConfigurationBuilder = null,
      ILogger logger = null)
    {
      return GetEventStoreRepositoryInternal(eventStoreConnection, eventTypeProvider, eventStoreRepositoryConfigurationBuilder, logger);
    }

    public static EventStoreRepository<TKey> GetEventStoreRepository<TKey>(
      string eventStoreUrl,
      ConnectionSettings settings,
      IEventTypeProvider<TKey> eventTypeProvider,
      Action<IEventStoreRepositoryConfiguration<TKey>> eventStoreRepositoryConfigurationBuilder = null,
      ILogger logger = null)
    {
      var eventStoreConnection = EventStoreConnection.Create(settings, new Uri(eventStoreUrl));

      return GetEventStoreRepositoryInternal(eventStoreConnection, eventTypeProvider, eventStoreRepositoryConfigurationBuilder, logger);
    }

    private static EventStoreRepository<TKey> GetEventStoreRepositoryInternal<TKey>(
      IEventStoreConnection eventStoreConnection,
      IEventTypeProvider<TKey> eventTypeProvider,
      Action<IEventStoreRepositoryConfiguration<TKey>> eventStoreRepositoryConfigurationBuilder = null,
      ILogger logger = null)
    {
      var connectionMonitor = new ConnectionStatusMonitor(eventStoreConnection, logger);

      var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration<TKey>();

      eventStoreRepositoryConfigurationBuilder?.Invoke(eventStoreRepositoryConfiguration);

      return new EventStoreRepository<TKey>(eventStoreRepositoryConfiguration,
        eventStoreConnection,
        connectionMonitor,
        eventTypeProvider,
        logger);

    }

    private static EventStoreCache<TKey, TCacheItem> GetEventStoreCacheInternal<TKey, TCacheItem>(
      IEventStoreConnection eventStoreConnection,
      IEventTypeProvider<TKey, TCacheItem> eventTypeProvider,
      Action<IEventStoreCacheConfiguration<TKey, TCacheItem>> eventStoreCacheConfigurationBuilder,
      ILogger logger = null)
      where TCacheItem : IAggregate<TKey>, new()
    {
      var connectionMonitor = new ConnectionStatusMonitor(eventStoreConnection, logger);

      var eventStoreCacheConfiguration = new EventStoreCacheConfiguration<TKey, TCacheItem>();

      eventStoreCacheConfigurationBuilder?.Invoke(eventStoreCacheConfiguration);

      return new EventStoreCache<TKey, TCacheItem>(connectionMonitor,
        eventStoreCacheConfiguration,
        eventTypeProvider,
        logger);

    }

  }
}
