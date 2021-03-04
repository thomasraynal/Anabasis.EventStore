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
using System.Threading.Tasks;
using System;

namespace Anabasis.Tests
{
  [TestFixture]
  public class TestPersistentQueue
  {
    private DebugLogger _debugLogger;
    private UserCredentials _userCredentials;
    private ConnectionSettings _connectionSettings;
    private ClusterVNode _clusterVNode;
    private (ConnectionStatusMonitor connectionStatusMonitor, PersistentSubscriptionEventStoreQueue persistentEventStoreQueue) _queueOne;
    private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreRepository<string> eventStoreRepository) _repositoryOne;
    private (ConnectionStatusMonitor connectionStatusMonitor, PersistentSubscriptionEventStoreQueue persistentEventStoreQueue) _queueTwo;

    private readonly string _streamId = "streamId";
    private readonly string _groupIdOne = "groupIdOne";
    private readonly string _groupIdTwo = "groupIdTwo";

    [OneTimeSetUp]
    public async Task Setup()
    {

      _debugLogger = new DebugLogger();
      _userCredentials = new UserCredentials("admin", "changeit");
      _connectionSettings = ConnectionSettings.Create().UseDebugLogger().KeepReconnecting().KeepRetrying().Build();

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

    private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreRepository<string> eventStoreRepository) CreateEventRepository()
    {
      var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration<string>(_userCredentials, _connectionSettings);
      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
      var connectionMonitor = new ConnectionStatusMonitor(connection, _debugLogger);

      var eventStoreRepository = new EventStoreRepository<string>(
        eventStoreRepositoryConfiguration,
        connection,
        connectionMonitor,
        new DefaultEventTypeProvider(() => new[] { typeof(SomeData<string>) }),
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


    [Test, Order(0)]
    public async Task ShouldCreateAndRunAVolatileEventStoreQueue()
    {
      _queueOne = CreatePersistentEventStoreQueue(_streamId, _groupIdOne);

      await Task.Delay(100);

      Assert.IsTrue(_queueOne.persistentEventStoreQueue.IsConnected);

    }

    [Test, Order(1)]
    public async Task ShouldCreateAndRunAnEventStoreRepositoryAndEmitOneEvent()
    {

      var eventCount = 0;

      _queueOne.persistentEventStoreQueue.OnEvent().Subscribe((@event) =>
      {
        eventCount++;
      });

      _repositoryOne = CreateEventRepository();

      await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_streamId));

      await Task.Delay(100);

      Assert.AreEqual(1, eventCount);

    }

    [Test, Order(2)]
    public async Task ShouldDropConnectionAndReinitializeIt()
    {

      var eventCount = 0;

      _queueOne.connectionStatusMonitor.ForceConnectionStatus(false);

      _queueOne.persistentEventStoreQueue.OnEvent().Subscribe((@event) =>
      {
        eventCount++;
      });

      await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_streamId));

      await Task.Delay(100);

      Assert.AreEqual(0, eventCount);

      _queueOne.connectionStatusMonitor.ForceConnectionStatus(true);

      await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_streamId));

      await Task.Delay(100);

      Assert.AreEqual(2, eventCount);

    }

    [Test, Order(3)]
    public async Task ShouldCreateASecondCacheAndCatchEvents()
    {
      var eventCountOne = 0;
      var eventCountTwo = 0;

      _queueOne.persistentEventStoreQueue.OnEvent().Subscribe((@event) =>
      {
        eventCountOne++;
      });

      _queueTwo = CreatePersistentEventStoreQueue(_streamId, _groupIdTwo);

      _queueTwo.persistentEventStoreQueue.OnEvent().Subscribe((@event) =>
      {
        eventCountTwo++;
      });

      await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_streamId));
      await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_streamId));
      await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_streamId));
      await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent(_streamId));

      await Task.Delay(100);

      Assert.True(eventCountOne> 0);
      Assert.True(eventCountTwo > 0);

    }
  }
}

