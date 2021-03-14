using Anabasis.Actor;
using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure;
using Anabasis.EventStore.Infrastructure.Queue;
using Anabasis.EventStore.Infrastructure.Queue.PersistentQueue;
using Anabasis.EventStore.Infrastructure.Queue.SubscribeFromEndQueue;
using Anabasis.Tests.Components;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using EventStore.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Tests
{
  public class SomeCommandResponse : BaseCommandResponse
  {

    public SomeCommandResponse(Guid commandId, Guid correlationId, string streamId) : base(commandId, correlationId, streamId)
    {
    }

    public override string Log()
    {
      throw new NotImplementedException();
    }
  }

  public class SomeCommand : BaseCommand
  {
 
    public SomeCommand(Guid correlationId, string streamId) : base(correlationId, streamId)
    {
    }

    public override string Log()
    {
      throw new NotImplementedException();
    }
  }

  public class TestActorReceiver : BaseActor
  {
    public TestActorReceiver(IEventStoreRepository eventStoreRepository) : base(eventStoreRepository)
    {

    }

    public async Task Handle(SomeCommand someCommand)
    {
      await Emit(new SomeCommandResponse(someCommand.EventID, someCommand.CorrelationID, someCommand.StreamId));
    }

    public override void Dispose()
    {
      base.Dispose();
    }

  }

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

    private Guid _correlationId = Guid.NewGuid();
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

    private (ConnectionStatusMonitor connectionStatusMonitor, SubscribeFromEndEventStoreQueue volatileEventStoreQueue) CreateVolatileEventStoreQueue()
    {
      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

      var connectionMonitor = new ConnectionStatusMonitor(connection, _debugLogger);

      var volatileEventStoreQueueConfiguration = new SubscribeFromEndEventStoreQueueConfiguration (_userCredentials);

      var volatileEventStoreQueue = new SubscribeFromEndEventStoreQueue(
        connectionMonitor,
        volatileEventStoreQueueConfiguration,
        new DefaultEventTypeProvider(() => new[] { typeof(SomeRandomEvent), typeof(SomeCommandResponse), typeof(SomeCommand) }),
        _debugLogger);

      return (connectionMonitor, volatileEventStoreQueue);

    }

    private (ConnectionStatusMonitor connectionStatusMonitor, IEventStoreRepository eventStoreRepository) CreateEventRepository()
    {
      var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration(_userCredentials);
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

      await _testActorOne.Emit(new SomeRandomEvent(_correlationId, _streamId));

      await Task.Delay(100);

      Assert.AreEqual(1, _testActorOne.Events.Count);

    }

    [Test, Order(2)]
    public async Task ShouldCreateASecondAndLoadBalanceEvents()
    {

      _testActorTwo = new TestActor(_eventRepository.eventStoreRepository);

      Assert.NotNull(_testActorOne);

      _queueTwo = CreatePersistentEventStoreQueue(_streamId, _groupIdOne);

      _testActorTwo.SubscribeTo(_queueTwo.persistentEventStoreQueue);

      var events = Enumerable.Range(0, 10).Select(_=>new SomeRandomEvent(_correlationId, _streamId)).ToArray();

      foreach(var ev in events)
      {
        await _eventRepository.eventStoreRepository.Emit(ev);
      }

      await Task.Delay(100);

      Assert.True(_testActorOne.Events.Count > 1);
      Assert.True(_testActorTwo.Events.Count > 1);

      var consumedEvents = _testActorOne.Events.Concat(_testActorTwo.Events).ToArray();

      Assert.True(events.All(ev => consumedEvents.Any(e => e.EventID == ev.EventID)));

    }

    [Test, Order(3)]
    public async Task ShouldSendACommand()
    {
      var (_, volatileEventStoreQueue) = CreateVolatileEventStoreQueue();

      var sender = new TestActor(_eventRepository.eventStoreRepository);
      sender.SubscribeTo(volatileEventStoreQueue);

      var receiver = new TestActorReceiver(_eventRepository.eventStoreRepository);
      receiver.SubscribeTo(volatileEventStoreQueue);

      var someCommandResponse = await sender.Send<SomeCommandResponse>(new SomeCommand(Guid.NewGuid(), "some-stream"), TimeSpan.FromSeconds(3));

      Assert.NotNull(someCommandResponse);
    }

  }
}
