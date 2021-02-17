using Anabasis.EventStore;
using Anabasis.EventStore.Infrastructure;
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
  public class TestsCatchupCache
  {
    private DebugLogger _debugLogger;
    private UserCredentials _userCredentials;
    private ConnectionSettings _connectionSettings;
    private ClusterVNode _clusterVNode;

    [OneTimeSetUp]
    public async Task Setup()
    {

      _debugLogger = new DebugLogger();
      _userCredentials = new UserCredentials("admin", "changeit");
      _connectionSettings = ConnectionSettings.Create().KeepReconnecting().KeepRetrying().Build();

      _clusterVNode = EmbeddedVNodeBuilder
        .AsSingleNode()
        .RunInMemory()
        .RunProjections(ProjectionType.All)
        .StartStandardProjections()
        .WithWorkerThreads(2)
        .Build();

      await _clusterVNode.StartAsync(true);



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
        new DefaultEventTypeProvider<Guid>((_) => typeof(SomeData)),
        _debugLogger);

      return (connectionMonitor, eventStoreRepository);
    }

    private (ConnectionStatusMonitor connectionStatusMonitor, CatchupEventStoreCache<Guid, SomeDataAggregate> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate> someDataAggregates) CreateCatchupEventStoreCache()
    {
      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

      var connectionMonitor = new ConnectionStatusMonitor(connection, _debugLogger);

      var cacheConfiguration = new CatchupEventStoreCacheConfiguration<Guid, SomeDataAggregate>()
      {
        UserCredentials = _userCredentials,
        KeepAppliedEventsOnAggregate = true
      };

      var catchUpCache = new CatchupEventStoreCache<Guid, SomeDataAggregate>(
        connectionMonitor,
        cacheConfiguration,
        new DefaultEventTypeProvider<Guid, SomeDataAggregate>((_) => typeof(SomeData)),
        _debugLogger);

      var aggregatesOnCacheOne = new ObservableCollectionExtended<SomeDataAggregate>();

      catchUpCache.AsObservableCache()
                     .Connect()
                     .Bind(aggregatesOnCacheOne)
                     .Subscribe();

      return (connectionMonitor, catchUpCache, aggregatesOnCacheOne);

    }

    [Test]
    public async Task ShouldCreateCatchupCacheAndRunIt()
    {

      var (connectionMonitorOne, catchupEventStoreCacheOne, aggregatesOnCacheOne) = CreateCatchupEventStoreCache();

      await Task.Delay(500);

      Assert.IsTrue(catchupEventStoreCacheOne.IsCaughtUp);
      Assert.IsFalse(catchupEventStoreCacheOne.IsStale);
      Assert.IsTrue(catchupEventStoreCacheOne.IsConnected);

      var (_, eventStoreRepository) = CreateEventRepository();

      var firstAggregateId = Guid.NewGuid();

      await eventStoreRepository.Emit(new SomeData(firstAggregateId));

      await Task.Delay(500);

      Assert.AreEqual(1, aggregatesOnCacheOne.Count);
      Assert.AreEqual(0, aggregatesOnCacheOne[0].Version);
      Assert.AreEqual(1, aggregatesOnCacheOne[0].AppliedEvents.Length);

      await eventStoreRepository.Emit(new SomeData(firstAggregateId));

      await Task.Delay(500);

      Assert.AreEqual(1, aggregatesOnCacheOne.Count);
      Assert.AreEqual(1, aggregatesOnCacheOne[0].Version);
      Assert.AreEqual(2, aggregatesOnCacheOne[0].AppliedEvents.Length);


      var (connectionMonitorTwo, catchupEventStoreCacheTwo, aggregatesOnCacheTwo) = CreateCatchupEventStoreCache();

      await Task.Delay(500);

      Assert.IsTrue(catchupEventStoreCacheOne.IsCaughtUp);
      Assert.IsFalse(catchupEventStoreCacheOne.IsStale);
      Assert.IsTrue(catchupEventStoreCacheOne.IsConnected);

      Assert.AreEqual(1, aggregatesOnCacheTwo.Count);
      Assert.AreEqual(1, aggregatesOnCacheTwo[0].Version);
      Assert.AreEqual(2, aggregatesOnCacheTwo[0].AppliedEvents.Length);

      var secondAggregateId = Guid.NewGuid();

      await eventStoreRepository.Emit(new SomeData(secondAggregateId));

      await Task.Delay(500);

      Assert.AreEqual(2, aggregatesOnCacheOne.Count);
      Assert.AreEqual(2, aggregatesOnCacheTwo.Count);

      connectionMonitorOne.ForceConnectionStatus(false);

      await Task.Delay(500);

      Assert.IsFalse(catchupEventStoreCacheOne.IsCaughtUp);
      Assert.IsTrue(catchupEventStoreCacheOne.IsStale);
      Assert.IsFalse(catchupEventStoreCacheOne.IsConnected);

      var thirdAggregateId = Guid.NewGuid();

      await eventStoreRepository.Emit(new SomeData(firstAggregateId));
      await eventStoreRepository.Emit(new SomeData(thirdAggregateId));

      await Task.Delay(500);

      Assert.AreEqual(2, aggregatesOnCacheOne.Count);
      Assert.AreEqual(2, aggregatesOnCacheOne[0].AppliedEvents.Length);

      Assert.AreEqual(3, aggregatesOnCacheTwo.Count);
      Assert.AreEqual(3, aggregatesOnCacheTwo[0].AppliedEvents.Length);

      connectionMonitorOne.ForceConnectionStatus(true);

      await Task.Delay(500);

      Assert.IsTrue(catchupEventStoreCacheOne.IsCaughtUp);
      Assert.IsFalse(catchupEventStoreCacheOne.IsStale);
      Assert.IsTrue(catchupEventStoreCacheOne.IsConnected);

      Assert.AreEqual(3, aggregatesOnCacheOne.Count);

      //remove rx bounded reconnection
      Assert.AreEqual(3, aggregatesOnCacheOne[0].AppliedEvents);


    }
  }
}
