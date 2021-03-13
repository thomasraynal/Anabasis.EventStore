using Anabasis.Actor;
using Anabasis.Actor.Actor;
using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure;
using Anabasis.EventStore.Infrastructure.Cache.CatchupSubscription;
using Anabasis.EventStore.Infrastructure.Repository;
using DynamicData;
using DynamicData.Binding;
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

  public class TestAggregateActorOne : BaseAggregateActor<Guid,SomeDataAggregate<Guid>>
  {
    public List<IEvent> Events { get; } = new List<IEvent>();

    public TestAggregateActorOne(IEventStoreAggregateRepository<Guid> eventStoreRepository, IEventStoreCache<Guid, SomeDataAggregate<Guid>> eventStoreCache, ISomeDependency _) : base(eventStoreRepository, eventStoreCache)
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

  public class TestAggregatedActorTwo : BaseAggregateActor<Guid, SomeDataAggregate<Guid>>
  {

    public List<IEvent> Events { get; } = new List<IEvent>();

    public TestAggregatedActorTwo(IEventStoreAggregateRepository<Guid> eventStoreRepository, IEventStoreCache<Guid, SomeDataAggregate<Guid>> eventStoreCache, ISomeDependency _) : base(eventStoreRepository, eventStoreCache)
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
  public class TestAggregateActorBuilder
  {
    private DebugLogger _debugLogger;
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

      await connection.CreatePersistentSubscriptionAsync(
           _streamId2,
           _groupIdOne,
           connectionSettings,
           _userCredentials);
    }


    [Test, Order(1)]
    public async Task ShouldBuildFromActorBuilderAndRunActors()
    {

      var testActorAutoBuildOne = AggregateActorBuilder<TestAggregateActorOne, Guid, SomeDataAggregate<Guid>, SomeRegistry>.Create(_clusterVNode, _userCredentials, _connectionSettings)
                                                                                   .WithReadAllFromStartCache(
                                                                                      catchupEventStoreCacheConfigurationBuilder: (conf)=> conf.KeepAppliedEventsOnAggregate = true,
                                                                                      eventTypeProvider: new DefaultEventTypeProvider(() => new[] { typeof(SomeData<Guid>) }))
                                                                                   .WithSubscribeToAllQueue()
                                                                                   .WithPersistentSubscriptionQueue(_streamId2, _groupIdOne)
                                                                                   .Build();

      var testActorAutoBuildTwo = AggregateActorBuilder<TestAggregatedActorTwo, Guid, SomeDataAggregate<Guid>, SomeRegistry>.Create(_clusterVNode, _userCredentials, _connectionSettings)
                                                                                   .WithReadAllFromStartCache(
                                                                                      catchupEventStoreCacheConfigurationBuilder: (conf) => conf.KeepAppliedEventsOnAggregate = true,
                                                                                      eventTypeProvider: new DefaultEventTypeProvider(() => new[] { typeof(SomeData<Guid>) }))
                                                                                   .Build();

      await testActorAutoBuildTwo.Emit(new SomeMoreData(_correlationId, "some-stream"));

      await Task.Delay(100);

      Assert.AreEqual(1, testActorAutoBuildOne.Events.Count);

      await testActorAutoBuildTwo.Emit(new SomeMoreData(_correlationId, _streamId2));

      await Task.Delay(100);

      Assert.AreEqual(3, testActorAutoBuildOne.Events.Count);

      var aggregateOne = Guid.NewGuid();
      var aggregateTwo = Guid.NewGuid();

      await testActorAutoBuildOne.EmitEntityEvent(new SomeData<Guid>(aggregateOne));
      await testActorAutoBuildOne.EmitEntityEvent(new SomeData<Guid>(aggregateTwo));

      await Task.Delay(500);

      Assert.AreEqual(2, testActorAutoBuildOne.State.GetCurrents().Length);
      Assert.AreEqual(2, testActorAutoBuildTwo.State.GetCurrents().Length);

      Assert.AreEqual(1, testActorAutoBuildOne.State.GetCurrent(aggregateOne).AppliedEvents.Length);
      Assert.AreEqual(1, testActorAutoBuildTwo.State.GetCurrent(aggregateTwo).AppliedEvents.Length);

      await testActorAutoBuildOne.EmitEntityEvent(new SomeData<Guid>(aggregateOne));
      await testActorAutoBuildOne.EmitEntityEvent(new SomeData<Guid>(aggregateTwo));

      await Task.Delay(100);

      Assert.AreEqual(2, testActorAutoBuildOne.State.GetCurrents().Length);
      Assert.AreEqual(2, testActorAutoBuildTwo.State.GetCurrents().Length);

      Assert.AreEqual(2, testActorAutoBuildOne.State.GetCurrent(aggregateOne).AppliedEvents.Length);
      Assert.AreEqual(2, testActorAutoBuildTwo.State.GetCurrent(aggregateTwo).AppliedEvents.Length);
    }

  }
}
