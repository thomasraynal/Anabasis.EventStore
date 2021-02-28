using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure;
using Anabasis.EventStore.Infrastructure.Cache.CatchupSubscription;
using Anabasis.EventStore.Infrastructure.Cache.VolatileSubscription;
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
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Tests
{
  [TestFixture]
  public class TestVolatileCache
  {
    private DebugLogger _debugLogger;
    private UserCredentials _userCredentials;
    private ConnectionSettings _connectionSettings;
    private ClusterVNode _clusterVNode;

    private (ConnectionStatusMonitor connectionStatusMonitor, VolatileEventStoreCache<string, SomeDataAggregate<string>> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate<string>> someDataAggregates) _cacheOne;
    private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreRepository<string> eventStoreRepository) _repositoryOne;
    private (ConnectionStatusMonitor connectionStatusMonitor, VolatileEventStoreCache<string, SomeDataAggregate<string>> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate<string>> someDataAggregates) _cacheTwo;

    private readonly string _streamIdOne = "streamIdOne";
    private readonly string _streamIdTwo = "streamIdTwo";

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
      var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration<string>(_userCredentials,_connectionSettings);
      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
      var connectionMonitor = new ConnectionStatusMonitor(connection, _debugLogger);

      var eventStoreRepository = new EventStoreRepository<string>(
        eventStoreRepositoryConfiguration,
        connection,
        connectionMonitor,
        new DefaultEventTypeProvider<string>(() => new[] { typeof(SomeData<string>) }),
        _debugLogger);

      return (connectionMonitor, eventStoreRepository);
    }

    private (ConnectionStatusMonitor connectionStatusMonitor, VolatileEventStoreCache<string, SomeDataAggregate<string>> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate<string>> someDataAggregates) CreateVolatileEventStoreCache()
    {
      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

      var connectionMonitor = new ConnectionStatusMonitor(connection, _debugLogger);

      var cacheConfiguration = new VolatileCacheConfiguration<string, SomeDataAggregate<string>>(_userCredentials)
      {
        KeepAppliedEventsOnAggregate = true,
        IsStaleTimeSpan = TimeSpan.FromSeconds(1)
      };

      var catchUpCache = new VolatileEventStoreCache<string, SomeDataAggregate<string>>(
        connectionMonitor,
        cacheConfiguration,
        new DefaultEventTypeProvider<string, SomeDataAggregate<string>>(() => new[] { typeof(SomeData<string>) }),
        _debugLogger);

      var aggregatesOnCacheOne = new ObservableCollectionExtended<SomeDataAggregate<string>>();

      catchUpCache.AsObservableCache()
                     .Connect()
                     .Bind(aggregatesOnCacheOne)
                     .Subscribe();

      return (connectionMonitor, catchUpCache, aggregatesOnCacheOne);

    }

    [Test, Order(0)]
    public async Task ShouldCreateAndRunAVolatileEventStoreCache()
    {
      _cacheOne = CreateVolatileEventStoreCache();

      await Task.Delay(100);

      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsCaughtUp);
      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsStale);
      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsConnected);

    }

    [Test, Order(1)]
    public async Task ShouldCreateAndRunAnEventStoreRepositoryAndEmitOneEvent()
    {
      _repositoryOne = CreateEventRepository();

      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamIdOne));

      await Task.Delay(100);

      Assert.AreEqual(1, _cacheOne.someDataAggregates.Count);
      Assert.AreEqual(0, _cacheOne.someDataAggregates[0].Version);
      Assert.AreEqual(1, _cacheOne.someDataAggregates[0].AppliedEvents.Length);
    }

    [Test, Order(2)]
    public async Task ShouldCreateASecondEventAndUpdateTheAggregate()
    {

      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamIdOne));

      await Task.Delay(100);

      Assert.AreEqual(1, _cacheOne.someDataAggregates.Count);
      Assert.AreEqual(1, _cacheOne.someDataAggregates[0].Version);
      Assert.AreEqual(2, _cacheOne.someDataAggregates[0].AppliedEvents.Length);

    }

    [Test, Order(3)]
    public async Task ShouldCreateASecondVolatileCache()
    {

      _cacheTwo = CreateVolatileEventStoreCache();

      await Task.Delay(100);

      Assert.IsTrue(_cacheTwo.catchupEventStoreCache.IsCaughtUp);
      Assert.IsTrue(_cacheTwo.catchupEventStoreCache.IsStale);
      Assert.IsTrue(_cacheTwo.catchupEventStoreCache.IsConnected);

      Assert.AreEqual(0, _cacheTwo.someDataAggregates.Count);

    }

    [Test, Order(4)]
    public async Task ShouldCreateASecondAggregate()
    {

      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamIdTwo));

      await Task.Delay(100);

      Assert.AreEqual(2, _cacheOne.someDataAggregates.Count);
      Assert.AreEqual(1, _cacheTwo.someDataAggregates.Count);

    }

    [Test, Order(5)]
    public async Task ShouldStopAndRestartVolatileCache()
    {

      _cacheOne.connectionStatusMonitor.ForceConnectionStatus(false);

      await Task.Delay(1500);

      Assert.IsFalse(_cacheOne.catchupEventStoreCache.IsCaughtUp);
      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsStale);
      Assert.IsFalse(_cacheOne.catchupEventStoreCache.IsConnected);

      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamIdOne));
      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamIdOne));

      await Task.Delay(100);

      Assert.AreEqual(2, _cacheOne.someDataAggregates.Count);
      Assert.AreEqual(2, _cacheOne.someDataAggregates[0].AppliedEvents.Length);

      Assert.AreEqual(2, _cacheTwo.someDataAggregates.Count);
      Assert.AreEqual(2, _cacheTwo.someDataAggregates[1].AppliedEvents.Length);

      _cacheOne.connectionStatusMonitor.ForceConnectionStatus(true);

      await Task.Delay(100);

      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsCaughtUp);
      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsStale);
      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsConnected);

      Assert.AreEqual(0, _cacheOne.someDataAggregates.Count);

    }

  }
}
