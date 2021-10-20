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
    public class TestMultipleStreamsCache
    {
        private UserCredentials _userCredentials;
        private ConnectionSettings _connectionSettings;
        private ClusterVNode _clusterVNode;
        private LoggerFactory _loggerFactory;

        private string _streamIdOne = "streamIdOne";
        private string _streamIdTwo = "streamIdTwo";
        private string _streamIdThree = "streamIdThree";

        private (ConnectionStatusMonitor connectionStatusMonitor, MultipleStreamsCatchupCache<string, SomeDataAggregate<string>> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate<string>> someDataAggregates) _multipleStreamsCatchupCache;
        private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository<Guid> eventStoreRepository) _eventStoreRepository;

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
                IsStaleTimeSpan = TimeSpan.FromSeconds(1)
            };

            var catchUpCache = new MultipleStreamsCatchupCache<string, SomeDataAggregate<string>>(
              connectionMonitor,
              cacheConfiguration,
              new DefaultEventTypeProvider<string, SomeDataAggregate<string>>(() => new[] { typeof(SomeData<string>) }),
              _loggerFactory);

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

            await Task.Delay(100);

            Assert.IsTrue(_multipleStreamsCatchupCache.catchupEventStoreCache.IsCaughtUp);
            Assert.IsTrue(_multipleStreamsCatchupCache.catchupEventStoreCache.IsStale);
            Assert.IsTrue(_multipleStreamsCatchupCache.catchupEventStoreCache.IsConnected);

        }

        [Test, Order(1)]
        public async Task ShouldCreateAndRunAnEventStoreRepositoryAndEmitOneEvent()
        {
            _eventStoreRepository = CreateEventRepository();

            await _eventStoreRepository.eventStoreRepository.Emit(new SomeData<string>(_streamIdOne, Guid.NewGuid()));

            await Task.Delay(100);

            Assert.AreEqual(1, _multipleStreamsCatchupCache.someDataAggregates.Count);
            Assert.AreEqual(0, _multipleStreamsCatchupCache.someDataAggregates[0].Version);
            Assert.AreEqual(1, _multipleStreamsCatchupCache.someDataAggregates[0].AppliedEvents.Length);
        }
    }
}
