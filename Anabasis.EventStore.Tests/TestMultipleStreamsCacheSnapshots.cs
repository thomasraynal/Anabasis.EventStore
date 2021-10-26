using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Snapshot;
using Anabasis.EventStore.Snapshot.InMemory;
using DynamicData;
using DynamicData.Binding;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using EventStore.Core;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Tests
{
    [TestFixture]
    public class TestMultipleStreamsCacheSnapshots
    {
        private UserCredentials _userCredentials;
        private ConnectionSettings _connectionSettings;
        private ClusterVNode _clusterVNode;
        private LoggerFactory _loggerFactory;

        private readonly string _streamIdOne = "streamIdOne";
        private readonly string _streamIdTwo = "streamIdTwo";
        private readonly string _streamIdThree = "streamIdThree";

        private (ConnectionStatusMonitor connectionStatusMonitor, MultipleStreamsCatchupCache<string, SomeDataAggregate<string>> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate<string>> someDataAggregates) _multipleStreamsCatchupCacheOne;
        private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository<Guid> eventStoreRepository) _eventStoreRepositoryAndConnectionMonitor;

        private ISnapshotStrategy<string> _defaultSnapshotStrategy;
        private ISnapshotStore<string, SomeDataAggregate<string>> _inMemorySnapshotStore;

        [OneTimeSetUp]
        public async Task Setup()
        {
            _userCredentials = new UserCredentials("admin", "changeit");

            _connectionSettings = ConnectionSettings.Create()
                .UseDebugLogger()
                .SetDefaultUserCredentials(_userCredentials)
                .KeepRetrying()
                .Build();

            _clusterVNode = EmbeddedVNodeBuilder
              .AsSingleNode()
              .RunInMemory()
              .RunProjections(ProjectionType.All)
              .StartStandardProjections()
              .WithWorkerThreads(1)
              .Build();

            await _clusterVNode.StartAsync(true);

            _loggerFactory = new LoggerFactory();

            _defaultSnapshotStrategy = new DefaultSnapshotStrategy<string>();
            _inMemorySnapshotStore = new InMemorySnapshotStore<string, SomeDataAggregate<string>>();

        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            await _clusterVNode.StopAsync();
        }

        private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository<Guid> eventStoreRepository) CreateEventRepository()
        {
            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();
            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
            var connectionMonitor = new ConnectionStatusMonitor(connection, _loggerFactory);

            var eventStoreRepository = new EventStoreAggregateRepository<Guid>(
              eventStoreRepositoryConfiguration,
              connection,
              connectionMonitor,
              _loggerFactory);

            return (connectionMonitor, eventStoreRepository);
        }

        private (ConnectionStatusMonitor connectionStatusMonitor, MultipleStreamsCatchupCache<string, SomeDataAggregate<string>> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate<string>> someDataAggregates) CreateCatchupEventStoreCache(params string[] streamIds)
        {
            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

            var connectionMonitor = new ConnectionStatusMonitor(connection, _loggerFactory);

            var cacheConfiguration = new MultipleStreamsCatchupCacheConfiguration<string, SomeDataAggregate<string>>(streamIds)
            {
                KeepAppliedEventsOnAggregate = true,
                IsStaleTimeSpan = TimeSpan.FromSeconds(1),
                UseSnapshot = true
            };

            var catchUpCache = new MultipleStreamsCatchupCache<string, SomeDataAggregate<string>>(
              connectionMonitor,
              cacheConfiguration,
              new DefaultEventTypeProvider<string, SomeDataAggregate<string>>(() => new[] { typeof(SomeData<string>) }),
              _loggerFactory,
              _inMemorySnapshotStore,
              _defaultSnapshotStrategy);

            catchUpCache.Connect();

            var aggregatesOnCacheOne = new ObservableCollectionExtended<SomeDataAggregate<string>>();

            catchUpCache.AsObservableCache()
                           .Connect()
                           .Bind(aggregatesOnCacheOne)
                           .Subscribe();

            return (connectionMonitor, catchUpCache, aggregatesOnCacheOne);

        }

        [Test, Order(0)]
        public async Task ShouldCreateAndRunAMultipleCatchupEventStoreCache()
        {
            _multipleStreamsCatchupCacheOne = CreateCatchupEventStoreCache(_streamIdOne, _streamIdTwo, _streamIdThree);

            await Task.Delay(1000);

            Assert.IsTrue(_multipleStreamsCatchupCacheOne.catchupEventStoreCache.IsCaughtUp);
            Assert.IsTrue(_multipleStreamsCatchupCacheOne.catchupEventStoreCache.IsStale);
            Assert.IsTrue(_multipleStreamsCatchupCacheOne.catchupEventStoreCache.IsConnected);

        }

        [Test, Order(1)]
        public async Task ShouldCreateAndRunAnEventStoreRepositoryAndEmitEventsAndCreateSnapshot()
        {
            _eventStoreRepositoryAndConnectionMonitor = CreateEventRepository();

            await _eventStoreRepositoryAndConnectionMonitor.eventStoreRepository.Emit(new SomeData<string>(_streamIdOne, Guid.NewGuid()));
            await _eventStoreRepositoryAndConnectionMonitor.eventStoreRepository.Emit(new SomeData<string>(_streamIdTwo, Guid.NewGuid()));
            await _eventStoreRepositoryAndConnectionMonitor.eventStoreRepository.Emit(new SomeData<string>(_streamIdThree, Guid.NewGuid()));

            await Task.Delay(500);

            Assert.AreEqual(3, _multipleStreamsCatchupCacheOne.someDataAggregates.Count);

            foreach (var aggregate in _multipleStreamsCatchupCacheOne.someDataAggregates)
            {
                Assert.AreEqual(0, aggregate.Version);
                Assert.AreEqual(1, aggregate.AppliedEvents.Length);
            }

            var subscriptionStates = _multipleStreamsCatchupCacheOne.catchupEventStoreCache.GetSubscriptionStates();

            foreach (var subscriptionState in subscriptionStates)
            {
                Assert.Null(subscriptionState.CurrentSnapshotEventVersion);
                Assert.NotNull(subscriptionState.LastProcessedEventSequenceNumber);
                Assert.NotNull(subscriptionState.LastProcessedEventUtcTimestamp);
                Assert.NotNull(subscriptionState.StreamId);
            }

            var nbOfEventUntilSnapshot = _defaultSnapshotStrategy.SnapshotIntervalInEvents -
                _multipleStreamsCatchupCacheOne.catchupEventStoreCache.GetCurrent(_streamIdOne).AppliedEvents.Length;

            for (var i = 0; i < nbOfEventUntilSnapshot; i++)
            {
                await _eventStoreRepositoryAndConnectionMonitor.eventStoreRepository.Emit(new SomeData<string>(_streamIdOne, Guid.NewGuid()));
            }

            await Task.Delay(500);

            var snapshot = await _inMemorySnapshotStore.GetAll();

            Assert.AreEqual(1, snapshot.Length);

            Assert.AreEqual(_streamIdOne, snapshot[0].EntityId);
            Assert.AreEqual(9, snapshot[0].Version);
            Assert.AreEqual(9, snapshot[0].VersionFromSnapshot);
        }

        [Test, Order(2)]
        public async Task ShouldDisconnectAndLoadSnapshot()
        {
            _multipleStreamsCatchupCacheOne.connectionStatusMonitor.ForceConnectionStatus(false);

            var streamOneEventCount = _multipleStreamsCatchupCacheOne.someDataAggregates
                    .First(aggregate => aggregate.EntityId == _streamIdOne)
                    .AppliedEvents.Length;

            await Task.Delay(500);

            Assert.AreEqual(false, _multipleStreamsCatchupCacheOne.catchupEventStoreCache.IsConnected);
            Assert.AreEqual(false, _multipleStreamsCatchupCacheOne.catchupEventStoreCache.IsCaughtUp);

            await _eventStoreRepositoryAndConnectionMonitor.eventStoreRepository.Emit(new SomeData<string>(_streamIdOne, Guid.NewGuid()));

            _multipleStreamsCatchupCacheOne.connectionStatusMonitor.ForceConnectionStatus(true);

            await Task.Delay(1000);

            var streamOneEventCountAfterDisconnect = _multipleStreamsCatchupCacheOne.someDataAggregates
                 .First(aggregate => aggregate.EntityId == _streamIdOne)
                 .AppliedEvents.Length;

            Assert.AreEqual(1, streamOneEventCountAfterDisconnect);

            var (connectionStatusMonitor, catchupEventStoreCache, someDataAggregates) = CreateCatchupEventStoreCache(_streamIdOne, _streamIdTwo, _streamIdThree);

            await Task.Delay(500);

            Assert.AreEqual(3, someDataAggregates.Count);

            var subscriptionHolders = catchupEventStoreCache.GetSubscriptionStates();

            Assert.AreEqual(3, subscriptionHolders.Length);

            var streamOneSubscriptionHolder = subscriptionHolders.First(subscriptionHolder => subscriptionHolder.StreamId == _streamIdOne);

            Assert.AreEqual(9, streamOneSubscriptionHolder.CurrentSnapshotEventVersion);
            Assert.AreEqual(streamOneSubscriptionHolder.CurrentSnapshotEventVersion + 1, streamOneSubscriptionHolder.LastProcessedEventSequenceNumber);
            Assert.NotNull(streamOneSubscriptionHolder.LastProcessedEventUtcTimestamp);
        }
    }
}
