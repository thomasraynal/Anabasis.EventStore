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
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Threading.Tasks;


namespace Anabasis.EventStore.Tests
{
    [TestFixture]
    public class TestsCatchupCache
    {
        private UserCredentials _userCredentials;
        private ConnectionSettings _connectionSettings;
        private ClusterVNode _clusterVNode;

        private (EventstoreConnectionStatusMonitor connectionStatusMonitor, AllStreamsCatchupCache<SomeDataAggregate> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate> someDataAggregates) _cacheOne;
        private (EventstoreConnectionStatusMonitor connectionStatusMonitor, AllStreamsCatchupCache<SomeDataAggregate> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate> someDataAggregates) _cacheTwo;
        private (EventstoreConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository eventStoreRepository) _repositoryOne;

        private ILoggerFactory _loggerFactory;

        private Guid _firstAggregateId;
        private Guid _secondAggregateId;
        private Guid _thirdAggregateId;

        [OneTimeSetUp]
        public async Task Setup()
        {

            _userCredentials = new UserCredentials("admin", "changeit");

            _connectionSettings = ConnectionSettings.Create()
                .UseDebugLogger()
                .SetDefaultUserCredentials(_userCredentials)
                .KeepRetrying()
                .Build();


            _loggerFactory = new LoggerFactory();

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

        private (EventstoreConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository eventStoreRepository) CreateEventRepository()
        {
            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();
            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
            var connectionMonitor = new EventstoreConnectionStatusMonitor(connection, _loggerFactory);

            var eventStoreRepository = new EventStoreAggregateRepository(
              eventStoreRepositoryConfiguration,
              connection,
              connectionMonitor,
              _loggerFactory);

            return (connectionMonitor, eventStoreRepository);
        }

        private (EventstoreConnectionStatusMonitor connectionStatusMonitor, AllStreamsCatchupCache<SomeDataAggregate> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate> someDataAggregates) CreateCatchupEventStoreCache()
        {
            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

            var connectionMonitor = new EventstoreConnectionStatusMonitor(connection, _loggerFactory);

            var cacheConfiguration = new AllStreamsCatchupCacheConfiguration<SomeDataAggregate>(Position.Start)
            {
                UserCredentials = _userCredentials,
                KeepAppliedEventsOnAggregate = true,
                IsStaleTimeSpan = TimeSpan.FromSeconds(1)
            };

            var catchUpCache = new AllStreamsCatchupCache<SomeDataAggregate>(
              connectionMonitor,
              cacheConfiguration,
              new DefaultEventTypeProvider<SomeDataAggregate>(() => new[] { typeof(SomeData) }),
             _loggerFactory);

            var aggregatesOnCacheOne = new ObservableCollectionExtended<SomeDataAggregate>();

            catchUpCache.AsObservableCache()
                           .Connect()
                           .Bind(aggregatesOnCacheOne)
                           .Subscribe();

            catchUpCache.Connect();

            return (connectionMonitor, catchUpCache, aggregatesOnCacheOne);

        }


        [Test, Order(0)]
        public async Task ShouldCreateAndRunACatchupEventStoreCache()
        {
            _cacheOne = CreateCatchupEventStoreCache();

            await Task.Delay(100);

            Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsCaughtUp);
            Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsStale);
            Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsConnected);

        }

        [Test, Order(1)]
        public async Task ShouldCreateAndRunAnEventStoreRepositoryAndEmitOneEvent()
        {
            _repositoryOne = CreateEventRepository();

            _firstAggregateId = Guid.NewGuid();

            await _repositoryOne.eventStoreRepository.Emit(new SomeData($"{_firstAggregateId}", Guid.NewGuid()));

            await Task.Delay(100);

            Assert.AreEqual(1, _cacheOne.someDataAggregates.Count);
            Assert.AreEqual(0, _cacheOne.someDataAggregates[0].Version);
            Assert.AreEqual(1, _cacheOne.someDataAggregates[0].AppliedEvents.Length);
        }

        [Test, Order(2)]
        public async Task ShouldCreateASecondEventAndUpdateTheAggregate()
        {

            await _repositoryOne.eventStoreRepository.Emit(new SomeData($"{_firstAggregateId}", Guid.NewGuid()));

            await Task.Delay(100);

            Assert.AreEqual(1, _cacheOne.someDataAggregates.Count);
            Assert.AreEqual(1, _cacheOne.someDataAggregates[0].Version);
            Assert.AreEqual(2, _cacheOne.someDataAggregates[0].AppliedEvents.Length);


        }

        [Test, Order(3)]
        public async Task ShouldCreateASecondCatchupCache()
        {

            _cacheTwo = CreateCatchupEventStoreCache();

            await Task.Delay(100);

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

            await _repositoryOne.eventStoreRepository.Emit(new SomeData($"{_secondAggregateId}", Guid.NewGuid()));

            await Task.Delay(100);

            Assert.AreEqual(2, _cacheTwo.someDataAggregates.Count);
            Assert.AreEqual(2, _cacheTwo.someDataAggregates.Count);

        }

        [Test, Order(5)]
        public async Task ShouldStopAndRestartCache()
        {

            await _cacheOne.catchupEventStoreCache.Disconnect();

            await Task.Delay(1500);

            Assert.IsFalse(_cacheOne.catchupEventStoreCache.IsCaughtUp);
            Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsStale);
            Assert.IsFalse(_cacheOne.catchupEventStoreCache.IsConnected);

            _thirdAggregateId = Guid.NewGuid();

            await _repositoryOne.eventStoreRepository.Emit(new SomeData($"{_firstAggregateId}", Guid.NewGuid()));
            await _repositoryOne.eventStoreRepository.Emit(new SomeData($"{_thirdAggregateId}", Guid.NewGuid()));

            await Task.Delay(100);

            Assert.AreEqual(2, _cacheOne.someDataAggregates.Count);
            Assert.AreEqual(2, _cacheOne.someDataAggregates[0].AppliedEvents.Length);

            Assert.AreEqual(3, _cacheTwo.someDataAggregates.Count);
            Assert.AreEqual(3, _cacheTwo.someDataAggregates[0].AppliedEvents.Length);

            await _cacheOne.catchupEventStoreCache.Connect();

            await Task.Delay(1000);

            Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsCaughtUp);
            Assert.IsTrue(_cacheOne.catchupEventStoreCache.IsConnected);

            Assert.AreEqual(3, _cacheOne.someDataAggregates.Count);

            Assert.AreEqual(3, _cacheOne.someDataAggregates[0].AppliedEvents.Length);

        }
    }
}
