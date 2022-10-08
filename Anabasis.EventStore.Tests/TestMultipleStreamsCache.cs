using Anabasis.Common;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
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
    public class TestMultipleStreamsCache
    {
        private UserCredentials _userCredentials;
        private ConnectionSettings _connectionSettings;
        private ClusterVNode _clusterVNode;
        private LoggerFactory _loggerFactory;

        private string _streamIdOne = "streamIdOne";
        private string _streamIdTwo = "streamIdTwo";
        private string _streamIdThree = "streamIdThree";

        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, MultipleStreamsCatchupCache<SomeDataAggregate> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate> someDataAggregates) _multipleStreamsCatchupCache;
        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository eventStoreRepository) _eventStoreRepository;

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

        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository eventStoreRepository) CreateEventRepository()
        {
            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();
            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
            var connectionMonitor = new EventStoreConnectionStatusMonitor(connection, _loggerFactory);

            var eventStoreRepository = new EventStoreAggregateRepository(
              eventStoreRepositoryConfiguration,
              connection,
              connectionMonitor,
              _loggerFactory);

            return (connectionMonitor, eventStoreRepository);
        }

        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, MultipleStreamsCatchupCache<SomeDataAggregate> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate> someDataAggregates) CreateCatchupEventStoreCache(params string[] streamIds)
        {
            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

            var connectionMonitor = new EventStoreConnectionStatusMonitor(connection, _loggerFactory);

            var cacheConfiguration = new MultipleStreamsCatchupCacheConfiguration(streamIds)
            {
                KeepAppliedEventsOnAggregate = true,
                IsStaleTimeSpan = TimeSpan.FromSeconds(1)
            };

            var catchUpCache = new MultipleStreamsCatchupCache<SomeDataAggregate>(
              connectionMonitor,
              cacheConfiguration,
              new DefaultEventTypeProvider<SomeDataAggregate>(() => new[] { typeof(SomeDataAggregateEvent) }),
              _loggerFactory);

            catchUpCache.ConnectToEventStream().Wait();

            var aggregatesOnCacheOne = new ObservableCollectionExtended<SomeDataAggregate>();

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

            await _eventStoreRepository.eventStoreRepository.Emit(new SomeDataAggregateEvent(_streamIdOne, Guid.NewGuid()));

            await Task.Delay(500);

            Assert.AreEqual(1, _multipleStreamsCatchupCache.someDataAggregates.Count);
            Assert.AreEqual(0, _multipleStreamsCatchupCache.someDataAggregates[0].Version);
            Assert.AreEqual(1, _multipleStreamsCatchupCache.someDataAggregates[0].AppliedEvents.Length);

            await _eventStoreRepository.eventStoreRepository.Emit(new SomeDataAggregateEvent(_streamIdTwo, Guid.NewGuid()));

            await Task.Delay(500);

            Assert.AreEqual(2, _multipleStreamsCatchupCache.someDataAggregates.Count);
            Assert.AreEqual(0, _multipleStreamsCatchupCache.someDataAggregates[0].Version);
            Assert.AreEqual(1, _multipleStreamsCatchupCache.someDataAggregates[1].AppliedEvents.Length);

        }

        [Test, Order(2)]
        public async Task ShouldRebuildCacheAfterHavingLostEventStoreConnection()
        {
            Assert.AreEqual(2, _multipleStreamsCatchupCache.someDataAggregates.Count);
            Assert.AreEqual(0, _multipleStreamsCatchupCache.someDataAggregates[0].Version);
            Assert.AreEqual(1, _multipleStreamsCatchupCache.someDataAggregates[1].AppliedEvents.Length);

            await _multipleStreamsCatchupCache.catchupEventStoreCache.DisconnectFromEventStream();

            await Task.Delay(500);

            await _eventStoreRepository.eventStoreRepository.Emit(new SomeDataAggregateEvent(_streamIdThree, Guid.NewGuid()));

            await Task.Delay(500);

            Assert.AreEqual(2, _multipleStreamsCatchupCache.someDataAggregates.Count);
            Assert.AreEqual(0, _multipleStreamsCatchupCache.someDataAggregates[0].Version);
            Assert.AreEqual(1, _multipleStreamsCatchupCache.someDataAggregates[1].AppliedEvents.Length);

            await Task.Delay(500);

            await _multipleStreamsCatchupCache.catchupEventStoreCache.ConnectToEventStream();

            await Task.Delay(500);

            Assert.AreEqual(3, _multipleStreamsCatchupCache.someDataAggregates.Count);
            Assert.AreEqual(0, _multipleStreamsCatchupCache.someDataAggregates[0].Version);
            Assert.AreEqual(1, _multipleStreamsCatchupCache.someDataAggregates[1].AppliedEvents.Length);

        }

    }
}
