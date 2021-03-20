using Anabasis.Actor;
using Anabasis.Actor.Actor;
using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure;
using Anabasis.EventStore.Infrastructure.Queue.PersistentQueue;
using Anabasis.EventStore.Infrastructure.Queue.SubscribeFromEndQueue;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using EventStore.Core;
using Lamar;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.Tests
{
  public class SomeDependency: ISomeDependency
  {

  }

  public interface ISomeDependency
  {
  }

  public class SomeRegistry : ServiceRegistry
  {
    public SomeRegistry()
    {
      For<ISomeDependency>().Use<SomeDependency>();
    }
  }

  public class TestActorAutoBuildOne : BaseActor
  {
    public List<IEvent> Events { get; } = new List<IEvent>();

    public TestActorAutoBuildOne(IEventStoreRepository eventStoreRepository, ISomeDependency _) : base(eventStoreRepository)
    {
    }

    public Task Handle(AgainSomeMoreData againSomeMoreData)
    {
      Events.Add(againSomeMoreData);

      return Task.CompletedTask;
    }

    public Task Handle(SomeMoreData someMoreData)
    {
      Events.Add(someMoreData);

      return Task.CompletedTask;
    }

  }

  public class TestActorAutoBuildTwo : BaseActor
  {

    public List<IEvent> Events { get; } = new List<IEvent>();

    public TestActorAutoBuildTwo(IEventStoreRepository eventStoreRepository, ISomeDependency _) : base(eventStoreRepository)
    {
    }
    public async Task Handle(SomeCommand someCommand)
    {
      await Emit(new SomeCommandResponse(someCommand.EventID, someCommand.CorrelationID, someCommand.StreamId));
    }

    public Task Handle(AgainSomeMoreData againSomeMoreData)
    {
      Events.Add(againSomeMoreData);

      return Task.CompletedTask;
    }

    public Task Handle(SomeMoreData someMoreData)
    {
      Events.Add(someMoreData);

      return Task.CompletedTask;
    }
  }


  [TestFixture]
  public class TestActorBuilder
  {

    private UserCredentials _userCredentials;
    private ConnectionSettings _connectionSettings;
    private ClusterVNode _clusterVNode;

    private Guid _correlationId = Guid.NewGuid();
    private readonly string _streamId = "streamId";
    private readonly string _streamId2 = "streamId2";
    private readonly string _groupIdOne = "groupIdOne";
    private readonly string _groupIdTwo = "groupIdTwo";

    [OneTimeSetUp]
    public async Task Setup()
    {

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

      await connection.CreatePersistentSubscriptionAsync(
           _streamId2,
           _groupIdOne,
           connectionSettings,
           _userCredentials);
    }


    [Test, Order(0)]
    public async Task ShouldBuildAndRunActors()
    {

      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
      var connectionMonitor = new ConnectionStatusMonitor(connection);

      var eventProvider = new ConsumerBasedEventProvider<TestActorAutoBuildOne>();

      var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration(_userCredentials);

      var eventStoreRepository = new EventStoreRepository(
        eventStoreRepositoryConfiguration,
        connection,
        connectionMonitor,
        new DefaultEventTypeProvider(() => new[] { typeof(SomeMoreData), typeof(AgainSomeMoreData) }));

      var persistentEventStoreQueueConfiguration = new PersistentSubscriptionEventStoreQueueConfiguration(_streamId, _groupIdOne, _userCredentials);

      var persistentSubscriptionEventStoreQueue = new PersistentSubscriptionEventStoreQueue(
        connectionMonitor,
        persistentEventStoreQueueConfiguration,
        eventProvider);

      var volatileEventStoreQueueConfiguration = new SubscribeFromEndEventStoreQueueConfiguration (_userCredentials);

      var volatileEventStoreQueue = new SubscribeFromEndEventStoreQueue(
        connectionMonitor,
        volatileEventStoreQueueConfiguration,
        eventProvider);

      var testActorAutoBuildOne = new TestActorAutoBuildOne(eventStoreRepository, new SomeDependency());
      var testActorAutoBuildTwo = new TestActorAutoBuildOne(eventStoreRepository, new SomeDependency());

      testActorAutoBuildOne.SubscribeTo(persistentSubscriptionEventStoreQueue);
      testActorAutoBuildOne.SubscribeTo(volatileEventStoreQueue);

      await testActorAutoBuildTwo.Emit(new SomeMoreData(_correlationId, "some-stream"));

      await Task.Delay(100);

      Assert.AreEqual(1, testActorAutoBuildOne.Events.Count);

      await testActorAutoBuildTwo.Emit(new SomeMoreData(_correlationId, _streamId));

      await Task.Delay(100);

      Assert.AreEqual(3, testActorAutoBuildOne.Events.Count);
    }

    [Test, Order(1)]
    public async Task ShouldBuildFromActorBuilderAndRunActors()
    {

      var testActorAutoBuildOne = ActorBuilder<TestActorAutoBuildOne, SomeRegistry>.Create(_clusterVNode, _userCredentials, _connectionSettings)
                                                                                   .WithSubscribeToAllQueue()
                                                                                   .WithPersistentSubscriptionQueue(_streamId2, _groupIdOne)
                                                                                   .Build();

      var testActorAutoBuildTwo = ActorBuilder<TestActorAutoBuildOne, SomeRegistry>.Create(_clusterVNode, _userCredentials, _connectionSettings)
                                                                                   .Build();

      await testActorAutoBuildTwo.Emit(new SomeMoreData(_correlationId, "some-stream"));

      await Task.Delay(100);

      Assert.AreEqual(1, testActorAutoBuildOne.Events.Count);

      await testActorAutoBuildTwo.Emit(new SomeMoreData(_correlationId, _streamId2));

      await Task.Delay(100);

      Assert.AreEqual(3, testActorAutoBuildOne.Events.Count);
    }

  }
}
