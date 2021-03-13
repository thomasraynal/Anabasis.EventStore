using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure;
using Anabasis.EventStore.Infrastructure.Cache.CatchupSubscription;
using Anabasis.EventStore.Infrastructure.Cache.VolatileSubscription;
using Anabasis.EventStore.Infrastructure.Queue;
using Anabasis.EventStore.Infrastructure.Queue.PersistentQueue;
using Anabasis.EventStore.Infrastructure.Queue.SubscribeFromEndQueue;
using Anabasis.EventStore.Infrastructure.Repository;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Core;
using Lamar;
using System;
using System.Collections.Generic;

namespace Anabasis.Actor.Actor
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

    public static AggregateActorBuilder<TActor, TKey, TAggregate, TRegistry> Create(ClusterVNode clusterVNode,
      UserCredentials userCredentials,
      ConnectionSettings connectionSettings,
      Action<IEventStoreRepositoryConfiguration> eventStoreRepositoryConfigurationBuilder = null,
      IEventTypeProvider eventTypeProvider = null,
      Microsoft.Extensions.Logging.ILogger logger = null)
    {

      var builder = new AggregateActorBuilder<TActor, TKey, TAggregate, TRegistry>();

      var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);

      builder._logger = logger;
      builder._userCredentials = userCredentials;
      builder._connectionMonitor = new ConnectionStatusMonitor(connection, logger);

      var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

      var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration(userCredentials, connectionSettings);

      eventStoreRepositoryConfigurationBuilder?.Invoke(eventStoreRepositoryConfiguration);

      builder._eventStoreRepository = new EventStoreAggregateRepository<TKey>(
        eventStoreRepositoryConfiguration,
        connection,
        builder._connectionMonitor,
        eventProvider,
        logger);

      return builder;

    }

    public AggregateActorBuilder<TActor, TKey, TAggregate, TRegistry> WithReadAllFromStartCache(
      Action<CatchupEventStoreCacheConfiguration<TKey, TAggregate>> catchupEventStoreCacheConfigurationBuilder = null,
      IEventTypeProvider eventTypeProvider = null)
    {
      if (null != _eventStoreCache) throw new InvalidOperationException($"A cache has already been set => {_eventStoreCache.GetType()}");

      var catchupEventStoreCacheConfiguration = new CatchupEventStoreCacheConfiguration<TKey, TAggregate>(_userCredentials);
      var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

      catchupEventStoreCacheConfigurationBuilder?.Invoke(catchupEventStoreCacheConfiguration);

      _eventStoreCache = new CatchupEventStoreCache<TKey, TAggregate>(_connectionMonitor, catchupEventStoreCacheConfiguration, eventProvider, _logger);

      return this;
    }

    public AggregateActorBuilder<TActor, TKey, TAggregate, TRegistry> WithReadOneStreamFromStartCache(
      string streamId,
      Action<SingleStreamCatchupEventStoreCacheConfiguration<TKey, TAggregate>> singleStreamCatchupEventStoreCacheConfigurationBuilder = null,
      IEventTypeProvider eventTypeProvider = null)
    {
      if (null != _eventStoreCache) throw new InvalidOperationException($"A cache has already been set => {_eventStoreCache.GetType()}");

      var singleStreamCatchupEventStoreCacheConfiguration = new SingleStreamCatchupEventStoreCacheConfiguration<TKey, TAggregate>(streamId,_userCredentials);
      var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

      singleStreamCatchupEventStoreCacheConfigurationBuilder?.Invoke(singleStreamCatchupEventStoreCacheConfiguration);

      _eventStoreCache = new SingleStreamCatchupEventStoreCache<TKey, TAggregate>(_connectionMonitor, singleStreamCatchupEventStoreCacheConfiguration, eventProvider, _logger);

      return this;
    }

    public AggregateActorBuilder<TActor, TKey, TAggregate, TRegistry> WithReadAllFromEndCache(
      Action<SubscribeFromEndCacheConfiguration<TKey, TAggregate>> volatileCacheConfigurationBuilder = null,
      IEventTypeProvider eventTypeProvider = null)
    {

      if (null != _eventStoreCache) throw new InvalidOperationException($"A cache has already been set => {_eventStoreCache.GetType()}");

      var volatileCacheConfiguration = new SubscribeFromEndCacheConfiguration<TKey, TAggregate>(_userCredentials);
      var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

      volatileCacheConfigurationBuilder?.Invoke(volatileCacheConfiguration);

      _eventStoreCache = new SubscribeFromEndEventStoreCache<TKey, TAggregate>(_connectionMonitor, volatileCacheConfiguration, eventProvider, _logger);

      return this;

    }

    public AggregateActorBuilder<TActor, TKey, TAggregate, TRegistry> WithSubscribeToAllQueue()
    {
      var volatileEventStoreQueueConfiguration = new SubscribeFromEndEventStoreQueueConfiguration (_userCredentials);

      var eventProvider = new ConsumerBasedEventProvider<TActor>();

      var volatileEventStoreQueue = new SubscribeFromEndEventStoreQueue(
        _connectionMonitor,
        volatileEventStoreQueueConfiguration,
        eventProvider,
        _logger);

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
        eventProvider,
        _logger);

      _queuesToRegisterTo.Add(persistentSubscriptionEventStoreQueue);

      return this;
    }

  }

}
