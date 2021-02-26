using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure;
using Anabasis.EventStore.Infrastructure.Cache.CatchupSubscription;
using DynamicData;
using DynamicData.Binding;
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
  public class TestsCatchupCache
  {
    private DebugLogger _debugLogger;
    private UserCredentials _userCredentials;
    private ConnectionSettings _connectionSettings;
    private ClusterVNode _clusterVNode;

    private (ConnectionStatusMonitor connectionStatusMonitor, CatchupEventStoreCache<Guid, SomeDataAggregate<Guid>> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate<Guid>> someDataAggregates) _cacheOne;
    private (ConnectionStatusMonitor connectionStatusMonitor, CatchupEventStoreCache<Guid, SomeDataAggregate<Guid>> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate<Guid>> someDataAggregates) _cacheTwo;
    private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreRepository<Guid> eventStoreRepository) _repositoryOne;

    private Guid _firstAggregateId;
    private Guid _secondAggregateId;
    private Guid _thirdAggregateId;

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

    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
      await _clusterVNode.StopAsync();
    }

    private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreRepository<Guid> eventStoreRepository) CreateEventRepository()
    {
      var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration<Guid>();
      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
      var connectionMonitor = new ConnectionStatusMonitor(connection, _debugLogger);

      var eventStoreRepository = new EventStoreRepository<Guid>(
        eventStoreRepositoryConfiguration,
        connection,
        connectionMonitor,
        new DefaultEventTypeProvider<Guid>(() => new[] { typeof(SomeData<Guid>) }),
        _debugLogger);

      return (connectionMonitor, eventStoreRepository);
    }

    private (ConnectionStatusMonitor connectionStatusMonitor, CatchupEventStoreCache<Guid, SomeDataAggregate<Guid>> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate<Guid>> someDataAggregates) CreateCatchupEventStoreCache()
    {
      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

      var connectionMonitor = new ConnectionStatusMonitor(connection, _debugLogger);

      var cacheConfiguration = new CatchupEventStoreCacheConfiguration<Guid, SomeDataAggregate<Guid>>(_userCredentials)
      {
        UserCredentials = _userCredentials,
        KeepAppliedEventsOnAggregate = true,
        IsStaleTimeSpan = TimeSpan.FromSeconds(2)
      };

      var catchUpCache = new CatchupEventStoreCache<Guid, SomeDataAggregate<Guid>>(
        connectionMonitor,
        cacheConfiguration,
       new DefaultEventTypeProvider<Guid, SomeDataAggregate<Guid>>(() => new[] { typeof(SomeData<Guid>) }),
        _debugLogger);

      var aggregatesOnCacheOne = new ObservableCollectionExtended<SomeDataAggregate<Guid>>();

      catchUpCache.AsObservableCache()
                     .Connect()
                     .Bind(aggregatesOnCacheOne)
                     .Subscribe();

      return (connectionMonitor, catchUpCache, aggregatesOnCacheOne);

    }
    [Test, Order(0)]
    public async Task ShouldCreateAndRunACatchupEventStoreCache()
    {
      _cacheOne = CreateCatchupEventStoreCache();

      await Task.Delay(500);

      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsCaughtUp);
      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsStale);
      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsConnected);

    }

    [Test, Order(1)]
    public async Task ShouldCreateAndRunAnEventStoreRepositoryAndEmitOneEvent()
    {
      _repositoryOne = CreateEventRepository();

     _firstAggregateId = Guid.NewGuid();

      await _repositoryOne.eventStoreRepository.Emit(new SomeData<Guid>(_firstAggregateId));

      await Task.Delay(500);

      Assert.AreEqual(1, _cacheOne.someDataAggregates.Count);
      Assert.AreEqual(0, _cacheOne.someDataAggregates[0].Version);
      Assert.AreEqual(1, _cacheOne.someDataAggregates[0].AppliedEvents.Length);
    }

    [Test, Order(2)]
    public async Task ShouldCreateASecondEventAndUpdateTheAggregate()
    {

      await _repositoryOne.eventStoreRepository.Emit(new SomeData<Guid>(_firstAggregateId));

      await Task.Delay(500);

      Assert.AreEqual(1, _cacheOne.someDataAggregates.Count);
      Assert.AreEqual(1, _cacheOne.someDataAggregates[0].Version);
      Assert.AreEqual(2, _cacheOne.someDataAggregates[0].AppliedEvents.Length);


    }

    [Test, Order(3)]
    public async Task ShouldCreateASecondCatchupCache()
    {

      _cacheTwo = CreateCatchupEventStoreCache();

      await Task.Delay(500);

      Assert.IsTrue(_cacheTwo.catchupEventStoreCache.IsCaughtUp);
      Assert.IsFalse(_cacheTwo.catchupEventStoreCache.IsStale);
      Assert.IsTrue(_cacheTwo.catchupEventStoreCache.IsConnected);

      Assert.AreEqual(1, _cacheTwo.someDataAggregates.Count);
      Assert.AreEqual(1, _cacheTwo.someDataAggregates[0].Version);
      Assert.AreEqual(2, _cacheTwo.someDataAggregates[0].AppliedEvents.Length);

    }

    [Test, Order(4)]
    public async Task ShouldCreateASecondAggregate()
    {

      _secondAggregateId = Guid.NewGuid();

      await _repositoryOne.eventStoreRepository.Emit(new SomeData<Guid>(_secondAggregateId));

      await Task.Delay(500);

      Assert.AreEqual(2, _cacheTwo.someDataAggregates.Count);
      Assert.AreEqual(2, _cacheTwo.someDataAggregates.Count);

    }

    [Test, Order(5)]
    public async Task ShouldStopAndRestartCache()
    {

      _cacheOne.connectionStatusMonitor.ForceConnectionStatus(false);

      await Task.Delay(500);

      Assert.IsFalse(_cacheOne.catchupEventStoreCache.IsCaughtUp);
      Assert.IsFalse(_cacheOne.catchupEventStoreCache.IsStale);
      Assert.IsFalse(_cacheOne.catchupEventStoreCache.IsConnected);

      await Task.Delay(2000);

      Assert.IsFalse(_cacheOne.catchupEventStoreCache.IsCaughtUp);
      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsStale);
      Assert.IsFalse(_cacheOne.catchupEventStoreCache.IsConnected);

      _thirdAggregateId = Guid.NewGuid();

      await _repositoryOne.eventStoreRepository.Emit(new SomeData<Guid>(_firstAggregateId));
      await _repositoryOne.eventStoreRepository.Emit(new SomeData<Guid>(_thirdAggregateId));

      await Task.Delay(500);

      Assert.AreEqual(2, _cacheOne.someDataAggregates.Count);
      Assert.AreEqual(2, _cacheOne.someDataAggregates[0].AppliedEvents.Length);

      Assert.AreEqual(3, _cacheTwo.someDataAggregates.Count);
      Assert.AreEqual(3, _cacheTwo.someDataAggregates[0].AppliedEvents.Length);

      _cacheOne.connectionStatusMonitor.ForceConnectionStatus(true);

      await Task.Delay(500);

      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsCaughtUp);
      Assert.IsFalse(_cacheOne.catchupEventStoreCache.IsStale);
      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsConnected);

      Assert.AreEqual(3, _cacheOne.someDataAggregates.Count);

      Assert.AreEqual(3, _cacheOne.someDataAggregates[0].AppliedEvents.Length);

    }
  }
}
