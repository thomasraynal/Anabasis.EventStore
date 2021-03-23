using Anabasis.Actor;
using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure;
using Anabasis.EventStore.Infrastructure.Cache.CatchupSubscription;
using Anabasis.EventStore.Infrastructure.Queue.PersistentQueue;
using Anabasis.EventStore.Infrastructure.Queue.SubscribeFromEndQueue;
using Anabasis.EventStore.Infrastructure.Repository;
using Anabasis.EventStore.Snapshot;
using Anabasis.Tests.Components;
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
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.Tests
{

  public class TestAggregateActorWithSnapshot : BaseAggregateActor<Guid, SomeDataAggregate<Guid>>
  {
    public TestAggregateActorWithSnapshot(SingleStreamCatchupEventStoreCache<Guid, SomeDataAggregate<Guid>> catchupEventStoreCache,
      IEventStoreAggregateRepository<Guid> eventStoreRepository) : base(eventStoreRepository, catchupEventStoreCache)
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
  public class TestActorWithSnapshot
  {

    private UserCredentials _userCredentials;
    private ConnectionSettings _connectionSettings;
    private ClusterVNode _clusterVNode;
    private (ConnectionStatusMonitor connectionStatusMonitor, IEventStoreAggregateRepository<Guid> eventStoreRepository) _eventRepository;

    private (ConnectionStatusMonitor connectionStatusMonitor,
      SingleStreamCatchupEventStoreCache<Guid, SomeDataAggregate<Guid>> catchupEventStoreCache,
      InMemorySnapshotStore<Guid, SomeDataAggregate<Guid>> inMemorySnapshotStore,
      DefaultSnapshotStrategy<Guid> defaultSnapshotStrategy,
      ObservableCollectionExtended<SomeDataAggregate<Guid>> someDataAggregates) _cache;

    private Guid _correlationId = Guid.NewGuid();
    private Guid _firstAggregateId = Guid.NewGuid();
    private (ConnectionStatusMonitor connectionStatusMonitor, SingleStreamCatchupEventStoreCache<Guid, SomeDataAggregate<Guid>> catchupEventStoreCache, InMemorySnapshotStore<Guid, SomeDataAggregate<Guid>> inMemorySnapshotStore, DefaultSnapshotStrategy<Guid> defaultSnapshotStrategy, ObservableCollectionExtended<SomeDataAggregate<Guid>> someDataAggregates) _secondCache;

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

    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
      await _clusterVNode.StopAsync();
    }

    private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository<Guid> eventStoreRepository) CreateEventRepository()
    {
      var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration(_userCredentials);
      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
      var connectionMonitor = new ConnectionStatusMonitor(connection);

      var eventStoreRepository = new EventStoreAggregateRepository<Guid>(
        eventStoreRepositoryConfiguration,
        connection,
        connectionMonitor,
        new DefaultEventTypeProvider(() => new[] { typeof(SomeData<Guid>) }));

      return (connectionMonitor, eventStoreRepository);
    }

    private (ConnectionStatusMonitor connectionStatusMonitor, SingleStreamCatchupEventStoreCache<Guid, SomeDataAggregate<Guid>> catchupEventStoreCache, InMemorySnapshotStore<Guid, SomeDataAggregate<Guid>> inMemorySnapshotStore, DefaultSnapshotStrategy<Guid> defaultSnapshotStrategy, ObservableCollectionExtended<SomeDataAggregate<Guid>> someDataAggregates) CreateCatchupEventStoreCache()
    {
      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

      var connectionMonitor = new ConnectionStatusMonitor(connection);

      var cacheConfiguration = new SingleStreamCatchupEventStoreCacheConfiguration<Guid, SomeDataAggregate<Guid>>($"{_firstAggregateId}", _userCredentials)
      {
        UserCredentials = _userCredentials,
        KeepAppliedEventsOnAggregate = true,
        IsStaleTimeSpan = TimeSpan.FromSeconds(1),
        UseSnapshot = true
      };

      var defaultSnapshotStrategy = new DefaultSnapshotStrategy<Guid>();
      var inMemorySnapshotStore = new InMemorySnapshotStore<Guid, SomeDataAggregate<Guid>>();

      var singleStreamCatchupEventStoreCache = new SingleStreamCatchupEventStoreCache<Guid, SomeDataAggregate<Guid>>(
        connectionMonitor,
        cacheConfiguration,
        snapshotStore: inMemorySnapshotStore,
        snapshotStrategy: defaultSnapshotStrategy,
        eventTypeProvider: new DefaultEventTypeProvider<Guid, SomeDataAggregate<Guid>>(() => new[] { typeof(SomeData<Guid>) }));

      var aggregatesOnCacheOne = new ObservableCollectionExtended<SomeDataAggregate<Guid>>();

      singleStreamCatchupEventStoreCache.AsObservableCache()
                     .Connect()
                     .Bind(aggregatesOnCacheOne)
                     .Subscribe();

      return (connectionMonitor, singleStreamCatchupEventStoreCache, inMemorySnapshotStore, defaultSnapshotStrategy, aggregatesOnCacheOne);

    }

    [Test, Order(0)]
    public async Task ShouldCreateAnActor()
    {

      var emitedEvents = new List<SomeData<Guid>>();

      _eventRepository = CreateEventRepository();
      _cache = CreateCatchupEventStoreCache();

      await Task.Delay(100);

      for (var i = 0; i < _cache.defaultSnapshotStrategy.SnapshotIntervalInEvents; i++)
      {
        var @event = new SomeData<Guid>(_firstAggregateId, _correlationId);

        emitedEvents.Add(@event);

        await _eventRepository.eventStoreRepository.Emit(@event);
      }

      await Task.Delay(500);

      var snapShots = await _cache.inMemorySnapshotStore.GetAll();

      Assert.AreEqual(1, snapShots.Length);

      Assert.AreEqual(9, snapShots[0].Version);
      Assert.AreEqual(9, snapShots[0].VersionSnapshot);


      for (var i = 0; i < _cache.defaultSnapshotStrategy.SnapshotIntervalInEvents; i++)
      {
        var @event = new SomeData<Guid>(_firstAggregateId, _correlationId);

        emitedEvents.Add(@event);

        await _eventRepository.eventStoreRepository.Emit(@event);
      }

      await Task.Delay(5000);

      snapShots = await _cache.inMemorySnapshotStore.GetAll();

      Assert.AreEqual(2, snapShots.Length);

      Assert.AreEqual(19, snapShots[1].Version);
      Assert.AreEqual(19, snapShots[1].VersionSnapshot);

      _secondCache = CreateCatchupEventStoreCache();

      await Task.Delay(100);

      Assert.AreEqual(19, _secondCache.catchupEventStoreCache.GetCurrent(_firstAggregateId).Version);
      Assert.AreEqual(19, _secondCache.catchupEventStoreCache.GetCurrent(_firstAggregateId).VersionSnapshot);

      await _eventRepository.eventStoreRepository.Emit(new SomeData<Guid>(_firstAggregateId, _correlationId));

      await Task.Delay(100);

      Assert.AreEqual(20, _secondCache.catchupEventStoreCache.GetCurrent(_firstAggregateId).Version);
      Assert.AreEqual(19, _secondCache.catchupEventStoreCache.GetCurrent(_firstAggregateId).VersionSnapshot);

    }

  }
}
