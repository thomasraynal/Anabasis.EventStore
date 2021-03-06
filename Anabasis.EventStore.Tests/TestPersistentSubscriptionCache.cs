//using Anabasis.EventStore.Cache;
//using Anabasis.EventStore.Connection;
//using Anabasis.EventStore.EventProvider;
//using Anabasis.EventStore.Repository;
//using DynamicData;
//using DynamicData.Binding;
//using EventStore.ClientAPI;
//using EventStore.ClientAPI.Embedded;
//using EventStore.ClientAPI.SystemData;
//using EventStore.Common.Options;
//using EventStore.Core;
//using NUnit.Framework;
//using System;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Anabasis.EventStore.Tests
//{
//    [TestFixture]
//  public class TestPersistentSubscriptionCache
//  {
//    private ClusterVNode _clusterVNode;
//    private UserCredentials _userCredentials;
//    private ConnectionSettings _connectionSettings;

//    private (ConnectionStatusMonitor connectionStatusMonitor, PersistentSubscriptionEventStoreCache<string, SomeDataAggregate<string>> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate<string>> someDataAggregates) _cacheOne;
//    private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository<string> eventStoreRepository) _repositoryOne;
//    private (ConnectionStatusMonitor connectionStatusMonitor, PersistentSubscriptionEventStoreCache<string, SomeDataAggregate<string>> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate<string>> someDataAggregates) _cacheTwo;

//    private readonly string _streamId = "streamId";
//    private readonly string _groupIdOne = "groupIdOne";
//    private readonly string _groupIdTwo = "groupIdTwo";

//    [OneTimeSetUp]
//    public async Task Setup()
//    {

//      _userCredentials = new UserCredentials("admin", "changeit");
//      _connectionSettings = ConnectionSettings.Create().UseDebugLogger().KeepRetrying().Build();

//      _clusterVNode = EmbeddedVNodeBuilder
//        .AsSingleNode()
//        .RunInMemory()
//        .RunProjections(ProjectionType.All)
//        .StartStandardProjections()
//        .WithWorkerThreads(1)
//        .Build();

//      await _clusterVNode.StartAsync(true);

//      await CreateSubscriptionGroups();

//    }

//    private async Task CreateSubscriptionGroups()
//    {
//      var connectionSettings = PersistentSubscriptionSettings.Create().StartFromCurrent().Build();
//      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode);

//      await connection.CreatePersistentSubscriptionAsync(
//           _streamId,
//           _groupIdOne,
//           connectionSettings,
//           _userCredentials);

//      await connection.CreatePersistentSubscriptionAsync(
//           _streamId,
//           _groupIdTwo,
//           connectionSettings,
//           _userCredentials);
//    }

//    private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository<string> eventStoreRepository) CreateEventRepository()
//    {
//      var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration(_userCredentials);
//      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
//      var connectionMonitor = new ConnectionStatusMonitor(connection);

//      var eventStoreRepository = new EventStoreAggregateRepository<string>(
//        eventStoreRepositoryConfiguration,
//        connection,
//        connectionMonitor,
//        new DefaultEventTypeProvider(() => new[] { typeof(SomeData<string>) }));

//      return (connectionMonitor, eventStoreRepository);
//    }

//    private (ConnectionStatusMonitor connectionStatusMonitor, PersistentSubscriptionEventStoreCache<string, SomeDataAggregate<string>> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate<string>> someDataAggregates) CreatePersistentEventStoreCache(string groupId)
//    {
//      var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

//      var connectionMonitor = new ConnectionStatusMonitor(connection);

//      var persistentSubscriptionCacheConfiguration = new PersistentSubscriptionCacheConfiguration<string, SomeDataAggregate<string>>(_streamId, groupId, _userCredentials)
//      {
//        KeepAppliedEventsOnAggregate = true,
//        IsStaleTimeSpan = TimeSpan.FromSeconds(1)
//      };

//      var catchUpCache = new PersistentSubscriptionEventStoreCache<string, SomeDataAggregate<string>>(
//        connectionMonitor,
//        persistentSubscriptionCacheConfiguration,
//        new DefaultEventTypeProvider<string, SomeDataAggregate<string>>(() => new[] { typeof(SomeData<string>) }));

//      var aggregatesOnCacheOne = new ObservableCollectionExtended<SomeDataAggregate<string>>();

//      catchUpCache.AsObservableCache()
//                     .Connect()
//                     .Bind(aggregatesOnCacheOne)
//                     .Subscribe();

//      return (connectionMonitor, catchUpCache, aggregatesOnCacheOne);

//    }


//    [Test, Order(0)]
//    public async Task ShouldCreateAndRunAPersistentEventStoreCache()
//    {
//      _cacheOne = CreatePersistentEventStoreCache(_groupIdOne);

//      await Task.Delay(100);

//      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsCaughtUp);
//      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsStale);
//      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsConnected);

//    }

//    [Test, Order(1)]
//    public async Task ShouldCreateAndRunAnEventStoreRepositoryAndEmitOneEvent()
//    {
//      _repositoryOne = CreateEventRepository();

//      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));

//      await Task.Delay(100);

//      Assert.AreEqual(1, _cacheOne.someDataAggregates.Count);
//      Assert.AreEqual(0, _cacheOne.someDataAggregates[0].Version);
//      Assert.AreEqual(1, _cacheOne.someDataAggregates[0].AppliedEvents.Length);

//    }

//    [Test, Order(2)]
//    public async Task ShouldCreateASecondCatchupCache()
//    {

//      _cacheTwo = CreatePersistentEventStoreCache(_groupIdOne);

//      await Task.Delay(100);

//      Assert.IsTrue(_cacheTwo.catchupEventStoreCache.IsCaughtUp);
//      Assert.IsTrue(_cacheTwo.catchupEventStoreCache.IsStale);
//      Assert.IsTrue(_cacheTwo.catchupEventStoreCache.IsConnected);

//      Assert.AreEqual(0, _cacheTwo.someDataAggregates.Count);

//    }

//    [Test, Order(3)]
//    public async Task ShouldEmitEventsAndLoadBalancedThem()
//    {
//      _repositoryOne = CreateEventRepository();

//      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));

//      await Task.Delay(100);

//      int getConsumedEventCount() => _cacheOne.someDataAggregates.Sum(aggregate => aggregate.AppliedEvents.Count()) +
//        _cacheTwo.someDataAggregates.Sum(aggregate => aggregate.AppliedEvents.Count());

//      Assert.AreEqual(2, getConsumedEventCount());

//      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));
//      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));
//      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));
//      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));
//      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));
//      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));
//      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));
//      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));

//      await Task.Delay(100);

//      Assert.AreEqual(10, getConsumedEventCount());

//    }

//    [Test, Order(4)]
//    public async Task ShouldStopAndRestartPersistentCache()
//    {
//      var eventsCountOnCacheOneBeforeDisconnect = _cacheOne.someDataAggregates.Sum(aggregate => aggregate.AppliedEvents.Count());

//      _cacheOne.connectionStatusMonitor.ForceConnectionStatus(false);

//      await Task.Delay(1500);

//      Assert.IsFalse(_cacheOne.catchupEventStoreCache.IsCaughtUp);
//      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsStale);
//      Assert.IsFalse(_cacheOne.catchupEventStoreCache.IsConnected);

//      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));
//      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));
//      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));
//      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));
//      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));
//      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));

//      await Task.Delay(100);

//      int getConsumedEventCount() => _cacheOne.someDataAggregates.Sum(aggregate => aggregate.AppliedEvents.Count()) +
//      _cacheTwo.someDataAggregates.Sum(aggregate => aggregate.AppliedEvents.Count());

//      Assert.AreEqual(16, getConsumedEventCount());
//      Assert.AreEqual(eventsCountOnCacheOneBeforeDisconnect, _cacheOne.someDataAggregates.Sum(aggregate => aggregate.AppliedEvents.Count()));

//      _cacheOne.connectionStatusMonitor.ForceConnectionStatus(true);

//      await Task.Delay(100);

//      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsCaughtUp);
//      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsStale);
//      Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsConnected);

//      Assert.AreEqual(0, _cacheOne.someDataAggregates.Count);

//      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));
//      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));
//      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));
//      await _repositoryOne.eventStoreRepository.Emit(new SomeData<string>(_streamId, Guid.NewGuid()));

//      await Task.Delay(100);

//      var eventOnCacheOne = 20 - (eventsCountOnCacheOneBeforeDisconnect + _cacheTwo.someDataAggregates.Sum(aggregate => aggregate.AppliedEvents.Count()));

//      Assert.AreEqual(eventOnCacheOne, _cacheOne.someDataAggregates.Sum(aggregate => aggregate.AppliedEvents.Count()));

//    }

//  }
//}
