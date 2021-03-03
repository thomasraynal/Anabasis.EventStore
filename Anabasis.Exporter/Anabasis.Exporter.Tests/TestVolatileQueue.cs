using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure;
using Anabasis.EventStore.Infrastructure.Queue;
using Anabasis.EventStore.Infrastructure.Queue.VolatileQueue;
using Anabasis.Tests.Components;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using EventStore.Core;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Anabasis.Tests
{
  [TestFixture]
  public class TestVolatileQueue
  {
    private DebugLogger _debugLogger;
    private UserCredentials _userCredentials;
    private ConnectionSettings _connectionSettings;
    private ClusterVNode _clusterVNode;
    private (ConnectionStatusMonitor connectionStatusMonitor, VolatileEventStoreQueue volatileEventStoreQueue) _queueOne;
    private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreRepository<string> eventStoreRepository) _repositoryOne;
    private (ConnectionStatusMonitor connectionStatusMonitor, VolatileEventStoreQueue volatileEventStoreQueue) _queueTwo;

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

    private (ConnectionStatusMonitor connectionStatusMonitor, VolatileEventStoreQueue volatileEventStoreQueue) CreateVolatileEventStoreQueue()
    {
      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

      var connectionMonitor = new ConnectionStatusMonitor(connection, _debugLogger);

      var volatileEventStoreQueueConfiguration = new VolatileEventStoreQueueConfiguration(_userCredentials);

      var volatileEventStoreQueue = new VolatileEventStoreQueue(
        connectionMonitor,
        volatileEventStoreQueueConfiguration,
        new DefaultEventTypeProvider(() => new[] { typeof(SomeRandomEvent) }),
        _debugLogger);

      return (connectionMonitor, volatileEventStoreQueue);

    }

    [Test, Order(0)]
    public async Task ShouldCreateAndRunAVolatileEventStoreQueue()
    {
      _queueOne = CreateVolatileEventStoreQueue();

      await Task.Delay(100);

      Assert.IsTrue(_queueOne.volatileEventStoreQueue.IsConnected);

    }

    [Test, Order(1)]
    public async Task ShouldCreateAndRunAnEventStoreRepositoryAndEmitOneEvent()
    {

      var eventCount = 0;

      _queueOne.volatileEventStoreQueue.OnEvent().Subscribe((@event) =>
      {
        eventCount++;
      });

      _repositoryOne = CreateEventRepository();

      await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent());

      await Task.Delay(100);

      Assert.AreEqual(1, eventCount);

    }

    [Test, Order(2)]
    public async Task ShouldDropConnectionAndReinitializeIt()
    {

      var eventCount = 0;

      _queueOne.connectionStatusMonitor.ForceConnectionStatus(false);

      _queueOne.volatileEventStoreQueue.OnEvent().Subscribe((@event) =>
       {
         eventCount++;
       });

      await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent());

      await Task.Delay(100);

      Assert.AreEqual(0, eventCount);

      _queueOne.connectionStatusMonitor.ForceConnectionStatus(true);

      await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent());

      await Task.Delay(100);

      Assert.AreEqual(1, eventCount);

    }

    [Test, Order(3)]
    public async Task ShouldCreateASecondCacheAndCatchEvents()
    {
      var eventCountOne = 0;
      var eventCountTwo = 0;

      _queueOne.volatileEventStoreQueue.OnEvent().Subscribe((@event) =>
      {
        eventCountOne++;
      });

      _queueTwo = CreateVolatileEventStoreQueue();

      _queueTwo.volatileEventStoreQueue.OnEvent().Subscribe((@event) =>
      {
        eventCountTwo++;
      });

      await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent());

      await Task.Delay(100);

      Assert.AreEqual(1, eventCountOne);
      Assert.AreEqual(1, eventCountTwo);

      await _repositoryOne.eventStoreRepository.Emit(new SomeRandomEvent());

      await Task.Delay(100);

      Assert.AreEqual(2, eventCountOne);
      Assert.AreEqual(2, eventCountTwo);
    }
  }
}
