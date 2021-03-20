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
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Anabasis.Tests
{
  public class CurrentState
  {
    public int EventCount { get; set; }
    public int HitCount { get; set; }

    public static CurrentState Default = new CurrentState();

  }

  [TestFixture]
  public class TestCacheSubscription
  {
    private UserCredentials _userCredentials;
    private ConnectionSettings _connectionSettings;
    private ClusterVNode _clusterVNode;

    private (ConnectionStatusMonitor connectionStatusMonitor, CatchupEventStoreCache<Guid, SomeDataAggregate<Guid>> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate<Guid>> someDataAggregates) _cacheOne;
    private (ConnectionStatusMonitor connectionStatusMonitor, CatchupEventStoreCache<Guid, SomeDataAggregate<Guid>> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate<Guid>> someDataAggregates) _cacheTwo;
    private (ConnectionStatusMonitor connectionStatusMonitor, IEventStoreAggregateRepository<Guid> eventStoreRepository) _repositoryOne;

    private Guid _firstAggregateId;

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

    private (ConnectionStatusMonitor connectionStatusMonitor, IEventStoreAggregateRepository<Guid> eventStoreRepository) CreateEventRepository()
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

    private (ConnectionStatusMonitor connectionStatusMonitor, CatchupEventStoreCache<Guid, SomeDataAggregate<Guid>> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate<Guid>> someDataAggregates) CreateCatchupEventStoreCache()
    {
      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

      var connectionMonitor = new ConnectionStatusMonitor(connection);

      var cacheConfiguration = new CatchupEventStoreCacheConfiguration<Guid, SomeDataAggregate<Guid>>(_userCredentials)
      {
        UserCredentials = _userCredentials,
        KeepAppliedEventsOnAggregate = true,
        IsStaleTimeSpan = TimeSpan.FromSeconds(2)
      };

      var catchUpCache = new CatchupEventStoreCache<Guid, SomeDataAggregate<Guid>>(
        connectionMonitor,
        cacheConfiguration,
       new DefaultEventTypeProvider<Guid, SomeDataAggregate<Guid>>(() => new[] { typeof(SomeData<Guid>) }));

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

      await Task.Delay(100);

      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsConnected);

    }

    [Test, Order(1)]
    public async Task ShouldCreateAndRunAnEventStoreRepositoryAndEmitOneEvent()
    {
      _repositoryOne = CreateEventRepository();

      _firstAggregateId = Guid.NewGuid();
      
      await _repositoryOne.eventStoreRepository.Emit(new SomeData<Guid>(_firstAggregateId));

      await Task.Delay(100);

      Assert.AreEqual(1, _cacheOne.someDataAggregates.Count);
      Assert.AreEqual(0, _cacheOne.someDataAggregates[0].Version);
      Assert.AreEqual(1, _cacheOne.someDataAggregates[0].AppliedEvents.Length);
    }


    [Test, Order(2)]
    public async Task ShouldSubscribeToCache()
    {

      var eventCount = 0;

      _cacheOne.catchupEventStoreCache.AsObservableCache()
                                      .Connect()
                                      .Subscribe(changeSet =>
                                      {
                                        eventCount++;
                                      });

      await Task.Delay(100);


      Assert.AreEqual(1, eventCount);

      await _repositoryOne.eventStoreRepository.Emit(new SomeData<Guid>(_firstAggregateId));

      await Task.Delay(100);

      Assert.AreEqual(2, eventCount);

    }


    [Test, Order(3)]
    public async Task ShouldCreateASecondcacheAndSubscribe()
    {
      _cacheTwo = CreateCatchupEventStoreCache();

      await Task.Delay(100);

      Assert.IsTrue(_cacheTwo.catchupEventStoreCache.IsConnected);

      var eventCount = 0;

      _cacheTwo.catchupEventStoreCache.AsObservableCache()
                                      .Connect()
                                      .Subscribe(changeSet =>
                                      {
                                        eventCount++;
                                      });

      await Task.Delay(100);

      Assert.AreEqual(1, eventCount);


    }


    [Test, Order(4)]
    public async Task ShouldToSomeObservableProjections()
    {
      _cacheTwo = CreateCatchupEventStoreCache();

      await Task.Delay(100);

      Assert.IsTrue(_cacheTwo.catchupEventStoreCache.IsConnected);

      var eventCount = 0;
      CurrentState currentState = null;

      _cacheTwo.catchupEventStoreCache.AsObservableCache()
                                      .Connect()
                                      .Scan(CurrentState.Default, (previous, obs) =>
                                      {
                                        return new CurrentState()
                                        {
                                          HitCount = previous.HitCount + 1,
                                          EventCount = obs.First().Current.AppliedEvents.Count()
                                        };

                                      })
                                      .Subscribe(state =>
                                      {
                                        eventCount++;
                                        currentState = state;
                                      });

      await Task.Delay(100);

      Assert.NotNull(currentState);
      Assert.AreEqual(1, eventCount);
      Assert.AreEqual(1, currentState.HitCount);
      Assert.AreEqual(2, currentState.EventCount);

      await _repositoryOne.eventStoreRepository.Emit(new SomeData<Guid>(_firstAggregateId));

      await Task.Delay(100);

      Assert.NotNull(currentState);
      Assert.AreEqual(2, eventCount);
      Assert.AreEqual(2, currentState.HitCount);
      Assert.AreEqual(3, currentState.EventCount);

    }

  }
}
