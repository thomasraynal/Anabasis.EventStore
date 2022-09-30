//using Anabasis.Common;
//using Anabasis.EventStore.Cache;
//using Anabasis.EventStore.Connection;
//using Anabasis.EventStore.Repository;
//using Anabasis.EventStore.Snapshot;
//using Anabasis.EventStore.Snapshot.InMemory;
//using DynamicData;
//using DynamicData.Binding;
//using EventStore.ClientAPI;
//using EventStore.ClientAPI.Embedded;
//using EventStore.ClientAPI.SystemData;
//using EventStore.Common.Options;
//using EventStore.Core;
//using Microsoft.Extensions.Logging;
//using NUnit.Framework;
//using System;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Anabasis.EventStore.Tests
//{
//    [TestFixture]
//    public class TestMultipleStreamsCacheSnapshots
//    {
//        private UserCredentials _userCredentials;
//        private ConnectionSettings _connectionSettings;
//        private ClusterVNode _clusterVNode;
//        private LoggerFactory _loggerFactory;

//        private readonly string _streamIdOne = $"{Guid.NewGuid()}";
//        private readonly string _streamIdTwo = $"{Guid.NewGuid()}";
//        private readonly string _streamIdThree = $"{Guid.NewGuid()}";

//        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, MultipleStreamsCatchupCache<SomeDataAggregate> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate> someDataAggregates) _multipleStreamsCatchupCacheOne;
//        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository eventStoreRepository) _eventStoreRepositoryAndConnectionMonitor;

//        private ISnapshotStrategy _defaultSnapshotStrategy;
//        private ISnapshotStore<SomeDataAggregate> _inMemorySnapshotStore;

//        [OneTimeSetUp]
//        public async Task Setup()
//        {
//            _userCredentials = new UserCredentials("admin", "changeit");

//            _connectionSettings = ConnectionSettings.Create()
//                .UseDebugLogger()
//                .SetDefaultUserCredentials(_userCredentials)
//                .KeepRetrying()
//                .Build();

//            _clusterVNode = EmbeddedVNodeBuilder
//              .AsSingleNode()
//              .RunInMemory()
//              .RunProjections(ProjectionType.All)
//              .StartStandardProjections()
//              .WithWorkerThreads(1)
//              .Build();

//            await _clusterVNode.StartAsync(true);

//            _loggerFactory = new LoggerFactory();

//            _defaultSnapshotStrategy = new DefaultSnapshotStrategy();
//            _inMemorySnapshotStore = new InMemorySnapshotStore<SomeDataAggregate>();

//        }

//        [OneTimeTearDown]
//        public async Task TearDown()
//        {
//            await _clusterVNode.StopAsync();
//        }

//        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository eventStoreRepository) CreateEventRepository()
//        {
//            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();
//            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
//            var connectionMonitor = new EventStoreConnectionStatusMonitor(connection, _loggerFactory);

//            var eventStoreRepository = new EventStoreAggregateRepository(
//              eventStoreRepositoryConfiguration,
//              connection,
//              connectionMonitor,
//              _loggerFactory);

//            return (connectionMonitor, eventStoreRepository);
//        }

//        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, MultipleStreamsCatchupCache<SomeDataAggregate> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate> someDataAggregates) CreateCatchupEventStoreCache(params string[] streamIds)
//        {
//            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

//            var connectionMonitor = new EventStoreConnectionStatusMonitor(connection, _loggerFactory);

//            var cacheConfiguration = new MultipleStreamsCatchupCacheConfiguration<SomeDataAggregate>(streamIds)
//            {
//                KeepAppliedEventsOnAggregate = true,
//                IsStaleTimeSpan = TimeSpan.FromSeconds(1),
//                UseSnapshot = true
//            };

//            var catchUpCache = new MultipleStreamsCatchupCache<SomeDataAggregate>(
//              connectionMonitor,
//              cacheConfiguration,
//              new DefaultEventTypeProvider<SomeDataAggregate>(() => new[] { typeof(SomeData) }),
//              _loggerFactory,
//              _inMemorySnapshotStore,
//              _defaultSnapshotStrategy);

//            catchUpCache.ConnectToEventStream().Wait();

//            var aggregatesOnCacheOne = new ObservableCollectionExtended<SomeDataAggregate>();

//            catchUpCache.AsObservableCache()
//                           .Connect()
//                           .Bind(aggregatesOnCacheOne)
//                           .Subscribe();

//            return (connectionMonitor, catchUpCache, aggregatesOnCacheOne);

//        }

//        [Test, Order(0)]
//        public async Task ShouldCreateAndRunAMultipleCatchupEventStoreCache()
//        {
//            _multipleStreamsCatchupCacheOne = CreateCatchupEventStoreCache(_streamIdOne, _streamIdTwo, _streamIdThree);

//            await Task.Delay(1000);

//            Assert.IsTrue(_multipleStreamsCatchupCacheOne.catchupEventStoreCache.IsCaughtUp);
//            Assert.IsTrue(_multipleStreamsCatchupCacheOne.catchupEventStoreCache.IsStale);
//            Assert.IsTrue(_multipleStreamsCatchupCacheOne.catchupEventStoreCache.IsConnected);

//        }

//        [Test, Order(1)]
//        public async Task ShouldCreateAndRunAnEventStoreRepositoryAndEmitEventsAndCreateSnapshot()
//        {
//            _eventStoreRepositoryAndConnectionMonitor = CreateEventRepository();

//            await _eventStoreRepositoryAndConnectionMonitor.eventStoreRepository.Emit(new SomeData(_streamIdOne, Guid.NewGuid()));
//            await _eventStoreRepositoryAndConnectionMonitor.eventStoreRepository.Emit(new SomeData(_streamIdTwo, Guid.NewGuid()));
//            await _eventStoreRepositoryAndConnectionMonitor.eventStoreRepository.Emit(new SomeData(_streamIdThree, Guid.NewGuid()));

//            await Task.Delay(500);

//            Assert.AreEqual(3, _multipleStreamsCatchupCacheOne.someDataAggregates.Count);

//            foreach (var aggregate in _multipleStreamsCatchupCacheOne.someDataAggregates)
//            {
//                Assert.AreEqual(0, aggregate.Version);
//                Assert.AreEqual(1, aggregate.AppliedEvents.Length);
//            }

//            var subscriptionStates = _multipleStreamsCatchupCacheOne.catchupEventStoreCache.GetSubscriptionStates();

//            foreach (var subscriptionState in subscriptionStates)
//            {
//                Assert.Null(subscriptionState.CurrentSnapshotEventVersion);
//                Assert.NotNull(subscriptionState.LastProcessedEventSequenceNumber);
//                Assert.NotNull(subscriptionState.LastProcessedEventUtcTimestamp);
//                Assert.NotNull(subscriptionState.StreamId);
//            }

//            var nbOfEventUntilSnapshot = _defaultSnapshotStrategy.SnapshotIntervalInEvents -
//                _multipleStreamsCatchupCacheOne.catchupEventStoreCache.GetCurrent(_streamIdOne).AppliedEvents.Length;

//            for (var i = 0; i < nbOfEventUntilSnapshot; i++)
//            {
//                await _eventStoreRepositoryAndConnectionMonitor.eventStoreRepository.Emit(new SomeData(_streamIdOne, Guid.NewGuid()));
//            }

//            await Task.Delay(1000);

//            var snapshots = (await _inMemorySnapshotStore.GetAll()).Where(snapshot => snapshot.EntityId == _streamIdOne).ToArray();

//            Assert.AreEqual(1, snapshots.Length);

//            Assert.AreEqual(_streamIdOne, snapshots[0].EntityId);
//            Assert.AreEqual(9, snapshots[0].Version);
//            Assert.AreEqual(9, snapshots[0].VersionFromSnapshot);
//        }

//        [Test, Order(2)]
//        public async Task ShouldDisconnectAndLoadSnapshot()
//        {
//            await _multipleStreamsCatchupCacheOne.catchupEventStoreCache.Disconnect();

//            var streamOneEventCount = _multipleStreamsCatchupCacheOne.someDataAggregates
//                    .First(aggregate => aggregate.EntityId == _streamIdOne)
//                    .AppliedEvents.Length;

//            await Task.Delay(500);

//            Assert.AreEqual(false, _multipleStreamsCatchupCacheOne.catchupEventStoreCache.IsCaughtUp);

//            await _eventStoreRepositoryAndConnectionMonitor.eventStoreRepository.Emit(new SomeData(_streamIdOne, Guid.NewGuid()));

//            await _multipleStreamsCatchupCacheOne.catchupEventStoreCache.ConnectToEventStream();

//            await Task.Delay(1000);

//            var streamOneEventCountAfterDisconnect = _multipleStreamsCatchupCacheOne.someDataAggregates
//                 .First(aggregate => aggregate.EntityId == _streamIdOne)
//                 .AppliedEvents.Length;

//            Assert.AreEqual(1, streamOneEventCountAfterDisconnect);

//            var (connectionStatusMonitor, catchupEventStoreCache, someDataAggregates) = CreateCatchupEventStoreCache(_streamIdOne, _streamIdTwo, _streamIdThree);

//            await Task.Delay(500);

//            Assert.AreEqual(3, someDataAggregates.Count);

//            var subscriptionHolders = catchupEventStoreCache.GetSubscriptionStates();

//            Assert.AreEqual(3, subscriptionHolders.Length);

//            var streamOneSubscriptionHolder = subscriptionHolders.First(subscriptionHolder => subscriptionHolder.StreamId == _streamIdOne);

//            Assert.AreEqual(9, streamOneSubscriptionHolder.CurrentSnapshotEventVersion);
//            Assert.AreEqual(streamOneSubscriptionHolder.CurrentSnapshotEventVersion + 1, streamOneSubscriptionHolder.LastProcessedEventSequenceNumber);
//            Assert.NotNull(streamOneSubscriptionHolder.LastProcessedEventUtcTimestamp);
//        }
//    }
//}
