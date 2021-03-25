using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Queue;
using Anabasis.EventStore.Repository;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Core;
using Lamar;
using System;
using System.Collections.Generic;

namespace Anabasis.EventStore.Actor
{
  public class ActorBuilder<TActor, TRegistry>
    where TActor : IActor
    where TRegistry : ServiceRegistry, new()
  {

    private EventStoreRepository _eventStoreRepository;
    private Microsoft.Extensions.Logging.ILogger _logger;
    private UserCredentials _userCredentials;
    private ConnectionStatusMonitor _connectionMonitor;

    private readonly List<IEventStoreQueue> _queuesToRegisterTo;

    private ActorBuilder()
    {
      _queuesToRegisterTo = new List<IEventStoreQueue>();
    }

    public TActor Build()
    {
      var container = new Container(configuration =>
      {
        configuration.For<IEventStoreRepository>().Use(_eventStoreRepository);
        configuration.For<IConnectionStatusMonitor>().Use(_connectionMonitor);
        configuration.IncludeRegistry<TRegistry>();
      });

      var actor = container.GetInstance<TActor>();

      foreach(var queue in _queuesToRegisterTo)
      {
        actor.SubscribeTo(queue, closeSubscriptionOnDispose: true);
      }

      return actor;

    }

    public static ActorBuilder<TActor, TRegistry> Create(ClusterVNode clusterVNode,
      UserCredentials userCredentials,
      ConnectionSettings connectionSettings,
      Action<IEventStoreRepositoryConfiguration> getEventStoreRepositoryConfiguration = null,
      IEventTypeProvider eventTypeProvider = null,
      Microsoft.Extensions.Logging.ILogger logger = null)
    {

      var builder = new ActorBuilder<TActor, TRegistry>();

      var connection = EmbeddedEventStoreConnection.Create(clusterVNode, connectionSettings);

      builder._logger = logger;
      builder._userCredentials = userCredentials;
      builder._connectionMonitor = new ConnectionStatusMonitor(connection, logger);

      var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

      var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration(userCredentials);

      getEventStoreRepositoryConfiguration?.Invoke(eventStoreRepositoryConfiguration);

      builder._eventStoreRepository = new EventStoreRepository(
        eventStoreRepositoryConfiguration,
        connection,
        builder._connectionMonitor,
        eventProvider,
        logger);

      return builder;

    }

    public ActorBuilder<TActor, TRegistry> WithSubscribeToAllQueue(IEventTypeProvider eventTypeProvider = null)
    {
      var volatileEventStoreQueueConfiguration = new SubscribeFromEndEventStoreQueueConfiguration (_userCredentials);

      var eventProvider = eventTypeProvider?? new ConsumerBasedEventProvider<TActor>();

      var volatileEventStoreQueue = new SubscribeFromEndEventStoreQueue(
        _connectionMonitor,
        volatileEventStoreQueueConfiguration,
        eventProvider);

      _queuesToRegisterTo.Add(volatileEventStoreQueue);

      return this;
    }

    public ActorBuilder<TActor, TRegistry> WithSubscribeToOneStreamQueue(string streamId, IEventTypeProvider eventTypeProvider = null)
    {
      var volatileEventStoreQueueConfiguration = new SubscribeFromEndEventStoreQueueConfiguration (_userCredentials);

      var eventProvider = eventTypeProvider ?? new ConsumerBasedEventProvider<TActor>();

      var volatileEventStoreQueue = new SubscribeFromEndEventStoreQueue(
        _connectionMonitor,
        volatileEventStoreQueueConfiguration,
        eventProvider);

      _queuesToRegisterTo.Add(volatileEventStoreQueue);

      return this;
    }

    public ActorBuilder<TActor, TRegistry> WithPersistentSubscriptionQueue(string streamId, string groupId)
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
