//using Anabasis.EventStore.Infrastructure;
//using Anabasis.EventStore.Infrastructure.Cache.CatchupSubscription;
//using EventStore.ClientAPI;
//using EventStore.ClientAPI.Embedded;
//using EventStore.Core;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.Text;
//using ILogger = Microsoft.Extensions.Logging.ILogger;

//namespace Anabasis.EventStore
//{

//  public static class EventStoreBuild
//  {

//    public static CatchupEventStoreCache<TKey, TAggregate> GetEventStoreCache<TKey, TAggregate>(
//      ClusterVNode clusterVNode,
//      ConnectionSettings settings,
//      IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
//      Action<IEventStoreCacheConfiguration<TKey, TAggregate>> eventStoreCacheConfigurationBuilder = null,
//      ILogger logger = null) where TAggregate : IAggregate<TKey>, new()
//    {
//      var eventStoreConnection = EmbeddedEventStoreConnection.Create(clusterVNode, settings);

//      return GetEventStoreCacheInternal(eventStoreConnection, eventTypeProvider, eventStoreCacheConfigurationBuilder, logger);
//    }

//    public static CatchupEventStoreCache<TKey, TAggregate> GetEventStoreCache<TKey, TAggregate>(
//      IEventStoreConnection eventStoreConnection,
//      IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
//      Action<IEventStoreCacheConfiguration<TKey, TAggregate>> eventStoreCacheConfigurationBuilder = null,
//      ILogger logger = null) where TAggregate : IAggregate<TKey>, new()
//    {
//      return GetEventStoreCacheInternal(eventStoreConnection, eventTypeProvider, eventStoreCacheConfigurationBuilder, logger);
//    }

//    public static CatchupEventStoreCache<TKey, TAggregate> GetEventStoreCache<TKey, TAggregate>(
//      string eventStoreUrl,
//      ConnectionSettings settings,
//      IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
//      Action<IEventStoreCacheConfiguration<TKey, TAggregate>> eventStoreCacheConfigurationBuilder = null,
//      ILogger logger = null) where TAggregate : IAggregate<TKey>, new()
//    {
//      var eventStoreConnection = EventStoreConnection.Create(settings, new Uri(eventStoreUrl));

//      return GetEventStoreCacheInternal(eventStoreConnection, eventTypeProvider, eventStoreCacheConfigurationBuilder, logger);
//    }

//    public static EventStoreRepository<TKey> GetEventStoreRepository<TKey>(
//      ClusterVNode clusterVNode,
//      ConnectionSettings settings,
//      IEventTypeProvider<TKey> eventTypeProvider,
//      Action<IEventStoreRepositoryConfiguration<TKey>> eventStoreRepositoryConfigurationBuilder = null,
//      ILogger logger = null)
//    {
//      var eventStoreConnection = EmbeddedEventStoreConnection.Create(clusterVNode, settings);

//      return GetEventStoreRepositoryInternal(eventStoreConnection, eventTypeProvider, eventStoreRepositoryConfigurationBuilder, logger);
//    }


//    public static EventStoreRepository<TKey> GetEventStoreRepository<TKey>(
//      IEventStoreConnection eventStoreConnection,
//      IEventTypeProvider<TKey> eventTypeProvider,
//      Action<IEventStoreRepositoryConfiguration<TKey>> eventStoreRepositoryConfigurationBuilder = null,
//      ILogger logger = null)
//    {
//      return GetEventStoreRepositoryInternal(eventStoreConnection, eventTypeProvider, eventStoreRepositoryConfigurationBuilder, logger);
//    }

//    public static EventStoreRepository<TKey> GetEventStoreRepository<TKey>(
//      string eventStoreUrl,
//      ConnectionSettings settings,
//      IEventTypeProvider<TKey> eventTypeProvider,
//      Action<IEventStoreRepositoryConfiguration<TKey>> eventStoreRepositoryConfigurationBuilder = null,
//      ILogger logger = null)
//    {
//      var eventStoreConnection = EventStoreConnection.Create(settings, new Uri(eventStoreUrl));

//      return GetEventStoreRepositoryInternal(eventStoreConnection, eventTypeProvider, eventStoreRepositoryConfigurationBuilder, logger);
//    }

//    private static EventStoreRepository<TKey> GetEventStoreRepositoryInternal<TKey>(
//      IEventStoreConnection eventStoreConnection,
//      IEventTypeProvider<TKey> eventTypeProvider,
//      Action<IEventStoreRepositoryConfiguration<TKey>> eventStoreRepositoryConfigurationBuilder = null,
//      ILogger logger = null)
//    {
//      var connectionMonitor = new ConnectionStatusMonitor(eventStoreConnection, logger);

//      var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration<TKey>();

//      eventStoreRepositoryConfigurationBuilder?.Invoke(eventStoreRepositoryConfiguration);

//      return new EventStoreRepository<TKey>(eventStoreRepositoryConfiguration,
//        eventStoreConnection,
//        connectionMonitor,
//        eventTypeProvider,
//        logger);

//    }

//    private static CatchupEventStoreCache<TKey, TAggregate> GetEventStoreCacheInternal<TKey, TAggregate>(
//      IEventStoreConnection eventStoreConnection,
//      IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
//      Action<IEventStoreCacheConfiguration<TKey, TAggregate>> eventStoreCacheConfigurationBuilder,
//      ILogger logger = null)
//      where TAggregate : IAggregate<TKey>, new()
//    {
//      var connectionMonitor = new ConnectionStatusMonitor(eventStoreConnection, logger);

//      var eventStoreCacheConfiguration = new CatchupEventStoreCacheConfiguration<TKey, TAggregate>(null);

//      eventStoreCacheConfigurationBuilder?.Invoke(eventStoreCacheConfiguration);

//      return new CatchupEventStoreCache<TKey, TAggregate>(connectionMonitor,
//        eventStoreCacheConfiguration,
//        eventTypeProvider,
//        logger);

//    }

//  }
//}
