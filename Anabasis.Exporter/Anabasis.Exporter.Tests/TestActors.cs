using Anabasis.Actor;
using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure;
using Anabasis.EventStore.Infrastructure.Queue.PersistentQueue;
using Anabasis.Tests.Components;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using EventStore.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.Tests
{

  public class TestActor : BaseActor
  {
    public TestActor(IEventStoreRepository eventStoreRepository) : base(eventStoreRepository)
    {
      Events = new List<SomeRandomEvent>();
    }

    public List<SomeRandomEvent> Events { get; }

    public Task Handle(SomeRandomEvent someMoreData)
    {
      Events.Add(someMoreData);

      return Task.CompletedTask;
    }

    public override void Dispose()
    {
      base.Dispose();
    }

  }

  [TestFixture]
  public class TestActors
  {

    private DebugLogger _debugLogger;
    private UserCredentials _userCredentials;
    private ConnectionSettings _connectionSettings;
    private ClusterVNode _clusterVNode;
    private (ConnectionStatusMonitor connectionStatusMonitor, IEventStoreRepository eventStoreRepository) _eventRepository;
    private TestActor _testActorOne;
    private (ConnectionStatusMonitor connectionStatusMonitor, PersistentSubscriptionEventStoreQueue persistentEventStoreQueue) _queueOne;
    private TestActor _testActorTwo;
    private (ConnectionStatusMonitor connectionStatusMonitor, PersistentSubscriptionEventStoreQueue persistentEventStoreQueue) _queueTwo;
    private readonly string _streamId = "streamId";
    private readonly string _groupIdOne = "groupIdOne";
    private readonly string _groupIdTwo = "groupIdTwo";

    [OneTimeSetUp]
    public async Task Setup()
    {

      _debugLogger = new DebugLogger();
      _userCredentials = new UserCredentials("admin", "changeit");
      _connectionSettings = ConnectionSettings.Create().UseDebugLogger().KeepRetrying().Build();

      _clusterVNode = EmbeddedVNodeBuilder
        .AsSingleNode()
        .RunInMemory()
        .RunProjections(ProjectionType.All)
        .StartStandardProjections()
        .WithWorkerThreads(1)
        .Build();

      await _clusterVNode.StartAsync(true);

      await CreateSubscriptionGroups();
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
      await _clusterVNode.StopAsync();
    }

    private async Task CreateSubscriptionGroups()
    {
      var connectionSettings = PersistentSubscriptionSettings.Create().StartFromCurrent().Build();
      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode);

      await connection.CreatePersistentSubscriptionAsync(
           _streamId,
           _groupIdOne,
           connectionSettings,
           _userCredentials);

      await connection.CreatePersistentSubscriptionAsync(
           _streamId,
           _groupIdTwo,
           connectionSettings,
           _userCredentials);
    }


    private (ConnectionStatusMonitor connectionStatusMonitor, IEventStoreRepository eventStoreRepository) CreateEventRepository()
    {
      var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration(_userCredentials, _connectionSettings);
      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
      var connectionMonitor = new ConnectionStatusMonitor(connection, _debugLogger);

      var eventStoreRepository = new EventStoreRepository(
        eventStoreRepositoryConfiguration,
        connection,
        connectionMonitor,
        new DefaultEventTypeProvider(() => new[] { typeof(SomeData<Guid>) }),
        _debugLogger);

      return (connectionMonitor, eventStoreRepository);
    }

    private (ConnectionStatusMonitor connectionStatusMonitor, PersistentSubscriptionEventStoreQueue persistentEventStoreQueue) CreatePersistentEventStoreQueue(string streamId, string groupId)
    {
      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

      var connectionMonitor = new ConnectionStatusMonitor(connection, _debugLogger);

      var persistentEventStoreQueueConfiguration = new PersistentSubscriptionEventStoreQueueConfiguration(streamId, groupId, _userCredentials);

      var persistentSubscriptionEventStoreQueue = new PersistentSubscriptionEventStoreQueue(
        connectionMonitor,
        persistentEventStoreQueueConfiguration,
        new DefaultEventTypeProvider(() => new[] { typeof(SomeRandomEvent) }),
        _debugLogger);

      return (connectionMonitor, persistentSubscriptionEventStoreQueue);

    }

    [Test, Order(0)]
    public async Task ShouldCreateAnActor()
    {
      _eventRepository = CreateEventRepository();

      await Task.Delay(100);

      _testActorOne = new TestActor(_eventRepository.eventStoreRepository);

      Assert.NotNull(_testActorOne);

    }

    [Test, Order(1)]
    public async Task ShouldCreateAQueueAndBindItToTheActor()
    {
      _eventRepository = CreateEventRepository();

      await Task.Delay(100);

      _testActorOne = new TestActor(_eventRepository.eventStoreRepository);

      Assert.NotNull(_testActorOne);

      _queueOne = CreatePersistentEventStoreQueue(_streamId, _groupIdOne);

      _testActorOne.SubscribeTo(_queueOne.persistentEventStoreQueue);

      _testActorOne.Emit(new SomeRandomEvent(_streamId));

      await Task.Delay(100);

      Assert.AreEqual(1, _testActorOne.Events.Count);

    }

    [Test, Order(2)]
    public async Task ShouldCreateASecondAndLoadBalanceEvents()
    {
  
      _testActorTwo = new TestActor(_eventRepository.eventStoreRepository);

      Assert.NotNull(_testActorOne);

      _queueTwo = CreatePersistentEventStoreQueue(_streamId, _groupIdTwo);

      _testActorTwo.SubscribeTo(_queueTwo.persistentEventStoreQueue);

      await _eventRepository.eventStoreRepository.Emit(new SomeRandomEvent(_streamId));
      await _eventRepository.eventStoreRepository.Emit(new SomeRandomEvent(_streamId));
      await _eventRepository.eventStoreRepository.Emit(new SomeRandomEvent(_streamId));
      await _eventRepository.eventStoreRepository.Emit(new SomeRandomEvent(_streamId));
      await _eventRepository.eventStoreRepository.Emit(new SomeRandomEvent(_streamId));
      await _eventRepository.eventStoreRepository.Emit(new SomeRandomEvent(_streamId));
      await _eventRepository.eventStoreRepository.Emit(new SomeRandomEvent(_streamId));
      await _eventRepository.eventStoreRepository.Emit(new SomeRandomEvent(_streamId));

      await Task.Delay(100);

      Assert.True(_testActorOne.Events.Count > 1);
      Assert.True(_testActorTwo.Events.Count > 1);

    }

  }
}
