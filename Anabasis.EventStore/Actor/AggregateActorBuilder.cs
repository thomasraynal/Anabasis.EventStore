using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Queue;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Snapshot;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Core;
using Lamar;
using System;
using System.Collections.Generic;

namespace Anabasis.EventStore.Actor
{
  public class AggregateActorBuilder<TActor, TKey, TAggregate, TRegistry>
    where TActor : IAggregateActor<TKey, TAggregate>
    where TAggregate : IAggregate<TKey>, new()
    where TRegistry : ServiceRegistry, new()
  {
 
    private IEventStoreCache<TKey, TAggregate> _eventStoreCache;
    private IEventStoreAggregateRepository<TKey> _eventStoreRepository;
    private Microsoft.Extensions.Logging.ILogger _logger;
    private UserCredentials _userCredentials;
    private ConnectionStatusMonitor _connectionMonitor;

    private readonly List<IEventStoreQueue> _queuesToRegisterTo;

    private AggregateActorBuilder()
    {
      _queuesToRegisterTo = new List<IEventStoreQueue>();
    }

    public TActor Build()
    {
      if (null == _eventStoreCache) throw new InvalidOperationException($"You must specify a cache for an AggregateActor");

      var container = new Container(configuration =>
      {
        configuration.For<IEventStoreCache<TKey,TAggregate>>().Use(_eventStoreCache);
        configuration.For<IEventStoreAggregateRepository<TKey>>().Use(_eventStoreRepository);
        configuration.For<IConnectionStatusMonitor>().Use(_connectionMonitor);
        configuration.IncludeRegistry<TRegistry>();

      });

      var actor = container.GetInstance<TActor>();

      foreach (var queue in _queuesToRegisterTo)
      {
        actor.SubscribeTo(queue, closeSubscriptionOnDispose: true);
      }

      return actor;

    }

    public static AggregateActorBuilder<TActor, TKey, TAggregate, TRegistry> Create(
    string eventStoreUrl,
    UserCredentials userCredentials,
    ConnectionSettings connectionSettings,
    IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
    Action<IEventStoreRepositoryConfiguration> eventStoreRepositoryConfigurationBuilder = null,
    Microsoft.Extensions.Logging.ILogger logger = null)
    {

      var connection = EventStoreConnection.Create(connectionSettings, new Uri(eventStoreUrl));

      return CreateInternal(connection, userCredentials, eventTypeProvider, eventStoreRepositoryConfigurationBuilder, logger);

    }


    public static AggregateActorBuilder<TActor, TKey, TAggregate, TRegistry> Create(ClusterVNode clusterVNode,
      UserCredentials userCredentials,
      ConnectionSettings connectionSettings,
      IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
      Action<IEventStoreRepositoryConfiguration> eventStoreRepositoryConfigurationBuilder = null,
      Microsoft.Extensions.Logging.ILogger logger = null)
    {

      var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);

      return CreateInternal(connection, userCredentials, eventTypeProvider, eventStoreRepositoryConfigurationBuilder, logger);

    }

    private static AggregateActorBuilder<TActor, TKey, TAggregate, TRegistry> CreateInternal(
      IEventStoreConnection eventStoreConnection,
      UserCredentials userCredentials,
      IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
      Action<IEventStoreRepositoryConfiguration> eventStoreRepositoryConfigurationBuilder = null,
      Microsoft.Extensions.Logging.ILogger logger = null)
    {

      var builder = new AggregateActorBuilder<TActor, TKey, TAggregate, TRegistry>
      {
        _logger = logger,
        _userCredentials = userCredentials,
        _connectionMonitor = new ConnectionStatusMonitor(eventStoreConnection, logger)
      };
      var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration(userCredentials);

      eventStoreRepositoryConfigurationBuilder?.Invoke(eventStoreRepositoryConfiguration);

      builder._eventStoreRepository = new EventStoreAggregateRepository<TKey>(
        eventStoreRepositoryConfiguration,
        eventStoreConnection,
        builder._connectionMonitor,
        eventTypeProvider,
        logger);

      return builder;

    }

    public AggregateActorBuilder<TActor, TKey, TAggregate, TRegistry> WithReadAllFromStartCache(
      IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
      Action<CatchupEventStoreCacheConfiguration<TKey, TAggregate>> catchupEventStoreCacheConfigurationBuilder = null,
      ISnapshotStore<TKey, TAggregate> snapshotStore = null,
      ISnapshotStrategy<TKey> snapshotStrategy = null)
    {
      if (null != _eventStoreCache) throw new InvalidOperationException($"A cache has already been set => {_eventStoreCache.GetType()}");

      var catchupEventStoreCacheConfiguration = new CatchupEventStoreCacheConfiguration<TKey, TAggregate>(_userCredentials);

      catchupEventStoreCacheConfigurationBuilder?.Invoke(catchupEventStoreCacheConfiguration);

      _eventStoreCache = new CatchupEventStoreCache<TKey, TAggregate>(_connectionMonitor, catchupEventStoreCacheConfiguration, eventTypeProvider, snapshotStore, snapshotStrategy);

      return this;
    }

    public AggregateActorBuilder<TActor, TKey, TAggregate, TRegistry> WithReadOneStreamFromStartCache(
      string streamId,
      IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
      Action<SingleStreamCatchupEventStoreCacheConfiguration<TKey, TAggregate>> singleStreamCatchupEventStoreCacheConfigurationBuilder = null,
      ISnapshotStore<TKey, TAggregate> snapshotStore = null,
      ISnapshotStrategy<TKey> snapshotStrategy = null)
    {
      if (null != _eventStoreCache) throw new InvalidOperationException($"A cache has already been set => {_eventStoreCache.GetType()}");

      var singleStreamCatchupEventStoreCacheConfiguration = new SingleStreamCatchupEventStoreCacheConfiguration<TKey, TAggregate>(streamId, _userCredentials);

      singleStreamCatchupEventStoreCacheConfigurationBuilder?.Invoke(singleStreamCatchupEventStoreCacheConfiguration);

      _eventStoreCache = new SingleStreamCatchupEventStoreCache<TKey, TAggregate>(_connectionMonitor, singleStreamCatchupEventStoreCacheConfiguration, eventTypeProvider, snapshotStore, snapshotStrategy);

      return this;
    }

    public AggregateActorBuilder<TActor, TKey, TAggregate, TRegistry> WithReadAllFromEndCache(
      IEventTypeProvider<TKey, TAggregate> eventTypeProvider,
      Action<SubscribeFromEndCacheConfiguration<TKey, TAggregate>> volatileCacheConfigurationBuilder = null)
    {

      if (null != _eventStoreCache) throw new InvalidOperationException($"A cache has already been set => {_eventStoreCache.GetType()}");

      var volatileCacheConfiguration = new SubscribeFromEndCacheConfiguration<TKey, TAggregate>(_userCredentials);

      volatileCacheConfigurationBuilder?.Invoke(volatileCacheConfiguration);

      _eventStoreCache = new SubscribeFromEndEventStoreCache<TKey, TAggregate>(_connectionMonitor, volatileCacheConfiguration, eventTypeProvider);

      return this;

    }

    public AggregateActorBuilder<TActor, TKey, TAggregate, TRegistry> WithSubscribeToAllQueue()
    {
      var volatileEventStoreQueueConfiguration = new SubscribeFromEndEventStoreQueueConfiguration (_userCredentials);

      var eventProvider = new ConsumerBasedEventProvider<TActor>();

      var volatileEventStoreQueue = new SubscribeFromEndEventStoreQueue(
        _connectionMonitor,
        volatileEventStoreQueueConfiguration,
        eventProvider);

      _queuesToRegisterTo.Add(volatileEventStoreQueue);

      return this;
    }

    public AggregateActorBuilder<TActor, TKey, TAggregate, TRegistry> WithPersistentSubscriptionQueue(string streamId, string groupId)
    {
      var persistentEventStoreQueueConfiguration = new PersistentSubscriptionEventStoreQueueConfiguration(streamId, groupId, _userCredentials);

      var eventProvider = new ConsumerBasedEventProvider<TActor>();

      var persistentSubscriptionEventStoreQueue = new PersistentSubscriptionEventStoreQueue(
        _connectionMonitor,
        persistentEventStoreQueueConfiguration,
        eventProvider);

      _queuesToRegisterTo.Add(persistentSubscriptionEventStoreQueue);

      return this;
    }

  }

}
