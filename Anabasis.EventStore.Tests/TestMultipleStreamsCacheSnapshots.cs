using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Snapshot;
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

        private (ConnectionStatusMonitor connectionStatusMonitor, MultipleStreamsCatchupCache<string, SomeDataAggregate<string>> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate<string>> someDataAggregates) _multipleStreamsCatchupCache;
        private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository<Guid> eventStoreRepository) _eventStoreRepository;

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

            _defaultSnapshotStrategy = new DefaultSnapshotStrategy<string>();
            _inMemorySnapshotStore = new InMemorySnapshotStore<string, SomeDataAggregate<string>>();

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
            _multipleStreamsCatchupCache = CreateCatchupEventStoreCache(_streamIdOne, _streamIdTwo, _streamIdThree);

            await Task.Delay(1000);

            Assert.IsTrue(_multipleStreamsCatchupCache.catchupEventStoreCache.IsCaughtUp);
            Assert.IsTrue(_multipleStreamsCatchupCache.catchupEventStoreCache.IsStale);
            Assert.IsTrue(_multipleStreamsCatchupCache.catchupEventStoreCache.IsConnected);

        }

        [Test, Order(1)]
        public async Task ShouldCreateAndRunAnEventStoreRepositoryAndEmitEvents()
        {
            _eventStoreRepository = CreateEventRepository();

            await _eventStoreRepository.eventStoreRepository.Emit(new SomeData<string>(_streamIdOne, Guid.NewGuid()));
            await _eventStoreRepository.eventStoreRepository.Emit(new SomeData<string>(_streamIdTwo, Guid.NewGuid()));
            await _eventStoreRepository.eventStoreRepository.Emit(new SomeData<string>(_streamIdThree, Guid.NewGuid()));

            await Task.Delay(500);

            Assert.AreEqual(3, _multipleStreamsCatchupCache.someDataAggregates.Count);

            foreach (var aggregate in _multipleStreamsCatchupCache.someDataAggregates)
            {
                Assert.AreEqual(0, aggregate.Version);
                Assert.AreEqual(1, aggregate.AppliedEvents.Length);
            }

            var subscriptionStates = _multipleStreamsCatchupCache.catchupEventStoreCache.GetSubscriptionStates();

            foreach (var subscriptionState in subscriptionStates)
            {
                Assert.Null(subscriptionState.CurrentSnapshotVersion);
                Assert.NotNull(subscriptionState.LastProcessedEventSequenceNumber);
                Assert.NotNull(subscriptionState.LastProcessedEventUtcTimestamp);
                Assert.NotNull(subscriptionState.StreamId);
            }

            var nbOfEventUntilSnapshot = _defaultSnapshotStrategy.SnapshotIntervalInEvents -
                _multipleStreamsCatchupCache.catchupEventStoreCache.GetCurrent(_streamIdOne).AppliedEvents.Length;

            for (var i = 0; i < nbOfEventUntilSnapshot; i++)
            {
                await _eventStoreRepository.eventStoreRepository.Emit(new SomeData<string>(_streamIdOne, Guid.NewGuid()));
            }

            await Task.Delay(500);

            var snapshot = await _inMemorySnapshotStore.GetAll();

            Assert.AreEqual(1, snapshot.Length);
   
            Assert.AreEqual(_streamIdOne, snapshot[0].EntityId);
            Assert.AreEqual(9, snapshot[0].Version);
            Assert.AreEqual(9, snapshot[0].VersionSnapshot);
        }

    }
}
