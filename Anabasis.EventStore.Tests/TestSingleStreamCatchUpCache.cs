using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Repository;
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

namespace Anabasis.EventStore.Tests
{
    [TestFixture]
  public class TestSingleStreamCatchUpCache
  {
    private UserCredentials _userCredentials;
    private ConnectionSettings _connectionSettings;
    private ClusterVNode _clusterVNode;

    private (ConnectionStatusMonitor connectionStatusMonitor, SingleStreamCatchupEventStoreCache<string, SomeDataAggregate<string>> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate<string>> someDataAggregates) _cacheOne;
    private (ConnectionStatusMonitor connectionStatusMonitor, SingleStreamCatchupEventStoreCache<string, SomeDataAggregate<string>> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate<string>> someDataAggregates) _cacheTwo;
    private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository<string> eventStoreRepository) _repositoryOne;

    private readonly string _streamId = "str";

    [OneTimeSetUp]
    public async Task Setup()
    {

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

    private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository<string> eventStoreRepository) CreateEventRepository()
    {
      var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration(_userCredentials);
      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
      var connectionMonitor = new ConnectionStatusMonitor(connection);

      var eventStoreRepository = new EventStoreAggregateRepository<string>(
        eventStoreRepositoryConfiguration,
        connection,
        connectionMonitor,
        new DefaultEventTypeProvider(() => new[] { typeof(SomeData<string>) }));

      return (connectionMonitor, eventStoreRepository);
    }

    private (ConnectionStatusMonitor connectionStatusMonitor, SingleStreamCatchupEventStoreCache<string, SomeDataAggregate<string>> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate<string>> someDataAggregates) CreateCatchupEventStoreCache(string streamId)
    {
      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

      var connectionMonitor = new ConnectionStatusMonitor(connection);

      var cacheConfiguration = new SingleStreamCatchupEventStoreCacheConfiguration<string, SomeDataAggregate<string>>(streamId, _userCredentials)
      {
        KeepAppliedEventsOnAggregate = true,
        IsStaleTimeSpan = TimeSpan.FromSeconds(1)
      };

      var catchUpCache = new SingleStreamCatchupEventStoreCache<string, SomeDataAggregate<string>>(
        connectionMonitor,
        cacheConfiguration,
        new DefaultEventTypeProvider<string, SomeDataAggregate<string>>(() => new[] { typeof(SomeData<string>) }));

      var aggregatesOnCacheOne = new ObservableCollectionExtended<SomeDataAggregate<string>>();

      catchUpCache.AsObservableCache()
                     .Connect()
                     .Bind(aggregatesOnCacheOne)
                     .Subscribe();

      return (connectionMonitor, catchUpCache, aggregatesOnCacheOne);

    }

    [Test, Order(0)]
    public async Task ShouldCreateAndRunACatchupEventStoreCache()
    {
      _cacheOne = CreateCatchupEventStoreCache(_streamId);

      await Task.Delay(100);

      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsCaughtUp);
      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsStale);
      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsConnected);

    }

    [Test, Order(1)]
    public async Task ShouldCreateAndRunAnEventStoreRepositoryAndEmitOneEvent()
    {
      _repositoryOne = CreateEventRepository();

      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));

      await Task.Delay(100);

      Assert.AreEqual(1, _cacheOne.someDataAggregates.Count);
      Assert.AreEqual(0, _cacheOne.someDataAggregates[0].Version);
      Assert.AreEqual(1, _cacheOne.someDataAggregates[0].AppliedEvents.Length);
    }

    [Test, Order(2)]
    public async Task ShouldCreateASecondEventAndUpdateTheAggregate()
    {

      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));

      await Task.Delay(100);

      Assert.AreEqual(1, _cacheOne.someDataAggregates.Count);
      Assert.AreEqual(1, _cacheOne.someDataAggregates[0].Version);
      Assert.AreEqual(2, _cacheOne.someDataAggregates[0].AppliedEvents.Length);

    }

    [Test, Order(3)]
    public async Task ShouldCreateASecondCatchupCache()
    {

      _cacheTwo = CreateCatchupEventStoreCache(_streamId);

      await Task.Delay(100);

      Assert.IsTrue(_cacheTwo.catchupEventStoreCache.IsCaughtUp);
      Assert.IsFalse(_cacheTwo.catchupEventStoreCache.IsStale);
      Assert.IsTrue(_cacheTwo.catchupEventStoreCache.IsConnected);

      Assert.AreEqual(1, _cacheTwo.someDataAggregates.Count);
      Assert.AreEqual(1, _cacheTwo.someDataAggregates[0].Version);
      Assert.AreEqual(2, _cacheTwo.someDataAggregates[0].AppliedEvents.Length);

    }

    [Test, Order(4)]
    public async Task ShouldEmitOneMoreEvent()
    {

      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));

      await Task.Delay(100);

      Assert.AreEqual(1, _cacheTwo.someDataAggregates.Count);
      Assert.AreEqual(1, _cacheTwo.someDataAggregates.Count);

    }

    [Test, Order(5)]
    public async Task ShouldStopAndRestartCache()
    {

      _cacheOne.connectionStatusMonitor.ForceConnectionStatus(false);

      await Task.Delay(100);

      Assert.IsFalse(_cacheOne.catchupEventStoreCache.IsCaughtUp);
      Assert.IsFalse(_cacheOne.catchupEventStoreCache.IsStale);
      Assert.IsFalse(_cacheOne.catchupEventStoreCache.IsConnected);

      await Task.Delay(1500);

      Assert.IsFalse(_cacheOne.catchupEventStoreCache.IsCaughtUp);
      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsStale);
      Assert.IsFalse(_cacheOne.catchupEventStoreCache.IsConnected);

      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));
      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));

      await Task.Delay(100);

      Assert.AreEqual(1, _cacheOne.someDataAggregates.Count);
      Assert.AreEqual(3, _cacheOne.someDataAggregates[0].AppliedEvents.Length);

      Assert.AreEqual(1, _cacheTwo.someDataAggregates.Count);
      Assert.AreEqual(5, _cacheTwo.someDataAggregates[0].AppliedEvents.Length);

      _cacheOne.connectionStatusMonitor.ForceConnectionStatus(true);

      await Task.Delay(100);

      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsCaughtUp);
      Assert.IsFalse(_cacheOne.catchupEventStoreCache.IsStale);
      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsConnected);

      Assert.AreEqual(1, _cacheOne.someDataAggregates.Count);

      Assert.AreEqual(5, _cacheOne.someDataAggregates[0].AppliedEvents.Length);

    }
  }
}
