using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Snapshot;
using Anabasis.EventStore.Tests.Components;
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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Tests
{

    public class TestStatefulActorWithSnapshot : BaseStatefulActor<Guid, SomeDataAggregate<Guid>>
    {
        public TestStatefulActorWithSnapshot(SingleStreamCatchupCache<Guid, SomeDataAggregate<Guid>> catchupEventStoreCache,
          IEventStoreAggregateRepository<Guid> eventStoreRepository) : base(eventStoreRepository, catchupEventStoreCache)
        {
            Events = new List<SomeRandomEvent>();
        }

        public List<SomeRandomEvent> Events { get; }

        public Task Handle(SomeRandomEvent someMoreData)
        {
            Events.Add(someMoreData);

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            base.Dispose();
        }

    }

    [TestFixture]
    public class TestActorWithSnapshot
    {

        private UserCredentials _userCredentials;
        private ConnectionSettings _connectionSettings;
        private ClusterVNode _clusterVNode;
        private (ConnectionStatusMonitor connectionStatusMonitor, IEventStoreAggregateRepository<Guid> eventStoreRepository) _eventRepository;

        private (ConnectionStatusMonitor connectionStatusMonitor,
          SingleStreamCatchupCache<Guid, SomeDataAggregate<Guid>> catchupEventStoreCache,
          InMemorySnapshotStore<Guid, SomeDataAggregate<Guid>> inMemorySnapshotStore,
          DefaultSnapshotStrategy<Guid> defaultSnapshotStrategy,
          ObservableCollectionExtended<SomeDataAggregate<Guid>> someDataAggregates) _cache;

        private Guid _correlationId = Guid.NewGuid();
        private Guid _firstAggregateId = Guid.NewGuid();
        private (ConnectionStatusMonitor connectionStatusMonitor, SingleStreamCatchupCache<Guid, SomeDataAggregate<Guid>> catchupEventStoreCache, InMemorySnapshotStore<Guid, SomeDataAggregate<Guid>> inMemorySnapshotStore, DefaultSnapshotStrategy<Guid> defaultSnapshotStrategy, ObservableCollectionExtended<SomeDataAggregate<Guid>> someDataAggregates) _secondCache;
        private ILoggerFactory _loggerFactory;

        [OneTimeSetUp]
        public async Task Setup()
        {

            _userCredentials = new UserCredentials("admin", "changeit");

            _connectionSettings = ConnectionSettings.Create()
               .UseDebugLogger()
               .SetDefaultUserCredentials(_userCredentials)
               .KeepRetrying()
               .Build();

            _loggerFactory = new DummyLoggerFactory();

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

        private (ConnectionStatusMonitor connectionStatusMonitor, SingleStreamCatchupCache<Guid, SomeDataAggregate<Guid>> catchupEventStoreCache, InMemorySnapshotStore<Guid, SomeDataAggregate<Guid>> inMemorySnapshotStore, DefaultSnapshotStrategy<Guid> defaultSnapshotStrategy, ObservableCollectionExtended<SomeDataAggregate<Guid>> someDataAggregates) CreateCatchupEventStoreCache()
        {
            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

            var connectionMonitor = new ConnectionStatusMonitor(connection, _loggerFactory);

            var cacheConfiguration = new SingleStreamCatchupCacheConfiguration<Guid, SomeDataAggregate<Guid>>($"{_firstAggregateId}", _userCredentials)
            {
                UserCredentials = _userCredentials,
                KeepAppliedEventsOnAggregate = true,
                IsStaleTimeSpan = TimeSpan.FromSeconds(1),
                UseSnapshot = true
            };

            var defaultSnapshotStrategy = new DefaultSnapshotStrategy<Guid>();
            var inMemorySnapshotStore = new InMemorySnapshotStore<Guid, SomeDataAggregate<Guid>>();

            var singleStreamCatchupEventStoreCache = new SingleStreamCatchupCache<Guid, SomeDataAggregate<Guid>>(
              connectionMonitor,
              cacheConfiguration,
              loggerFactory: _loggerFactory,
              snapshotStore: inMemorySnapshotStore,
              snapshotStrategy: defaultSnapshotStrategy,
              eventTypeProvider: new DefaultEventTypeProvider<Guid, SomeDataAggregate<Guid>>(() => new[] { typeof(SomeData<Guid>) }));

            var aggregatesOnCacheOne = new ObservableCollectionExtended<SomeDataAggregate<Guid>>();

            singleStreamCatchupEventStoreCache.AsObservableCache()
                           .Connect()
                           .Bind(aggregatesOnCacheOne)
                           .Subscribe();

            singleStreamCatchupEventStoreCache.Connect();

            return (connectionMonitor, singleStreamCatchupEventStoreCache, inMemorySnapshotStore, defaultSnapshotStrategy, aggregatesOnCacheOne);

        }

        [Test, Order(0)]
        public async Task ShouldCreateAnActor()
        {

            var emitedEvents = new List<SomeData<Guid>>();

            _eventRepository = CreateEventRepository();
            _cache = CreateCatchupEventStoreCache();

            await Task.Delay(100);

            for (var i = 0; i < _cache.defaultSnapshotStrategy.SnapshotIntervalInEvents; i++)
            {
                var @event = new SomeData<Guid>(_firstAggregateId, _correlationId);

                emitedEvents.Add(@event);

                await _eventRepository.eventStoreRepository.Emit(@event);
            }

            await Task.Delay(500);

            var snapShots = await _cache.inMemorySnapshotStore.GetAll();

            Assert.AreEqual(1, snapShots.Length);

            Assert.AreEqual(9, snapShots[0].Version);
            Assert.AreEqual(9, snapShots[0].VersionFromSnapshot);


            for (var i = 0; i < _cache.defaultSnapshotStrategy.SnapshotIntervalInEvents; i++)
            {
                var @event = new SomeData<Guid>(_firstAggregateId, _correlationId);

                emitedEvents.Add(@event);

                await _eventRepository.eventStoreRepository.Emit(@event);
            }

            await Task.Delay(5000);

            snapShots = await _cache.inMemorySnapshotStore.GetAll();

            Assert.AreEqual(2, snapShots.Length);

            Assert.AreEqual(19, snapShots[1].Version);
            Assert.AreEqual(19, snapShots[1].VersionFromSnapshot);

            _secondCache = CreateCatchupEventStoreCache();

            await Task.Delay(100);

            Assert.AreEqual(19, _secondCache.catchupEventStoreCache.GetCurrent(_firstAggregateId).Version);
            Assert.AreEqual(19, _secondCache.catchupEventStoreCache.GetCurrent(_firstAggregateId).VersionFromSnapshot);

            await _eventRepository.eventStoreRepository.Emit(new SomeData<Guid>(_firstAggregateId, _correlationId));

            await Task.Delay(100);

            Assert.AreEqual(20, _secondCache.catchupEventStoreCache.GetCurrent(_firstAggregateId).Version);
            Assert.AreEqual(19, _secondCache.catchupEventStoreCache.GetCurrent(_firstAggregateId).VersionFromSnapshot);

        }

    }
}
