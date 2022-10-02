using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Stream;
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
using System.Collections.Generic;
using System.Threading.Tasks;
using Anabasis.Common;
using Anabasis.EventStore.Snapshot;
using Anabasis.Common.Configuration;

namespace Anabasis.EventStore.Tests
{
    public class TestStatefulActor : SubscribeToAllStreamsEventStoreStatefulActor<SomeDataAggregate>
    {
        public TestStatefulActor(IActorConfigurationFactory actorConfigurationFactory, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, ILoggerFactory loggerFactory = null, ISnapshotStore<SomeDataAggregate> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, IKillSwitch killSwitch = null) : base(actorConfigurationFactory, connectionMonitor, loggerFactory, snapshotStore, snapshotStrategy, killSwitch)
        {
        }

        public TestStatefulActor(IActorConfiguration actorConfiguration, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, AllStreamsCatchupCacheConfiguration catchupCacheConfiguration, IEventTypeProvider eventTypeProvider, ILoggerFactory loggerFactory = null, ISnapshotStore<SomeDataAggregate> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, IKillSwitch killSwitch = null) : base(actorConfiguration, connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy, killSwitch)
        {
        }

        public List<SomeRandomEvent> Events { get; } = new List<SomeRandomEvent>();

        public Task Handle(SomeRandomEvent someMoreData)
        {
            Events.Add(someMoreData);

            return Task.CompletedTask;
        }

    }

    [TestFixture]
    public class TestStatefulActors
    {

        private UserCredentials _userCredentials;
        private ConnectionSettings _connectionSettings;
        private LoggerFactory _loggerFactory;
        private ClusterVNode _clusterVNode;

        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, AllStreamsCatchupCache<SomeDataAggregate> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate> someDataAggregates) _cacheOne;
        private EventStoreBus _eventStoreBus;
        private Guid _firstAggregateId = Guid.NewGuid();

        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository eventStoreRepository) _eventRepository;
        private TestStatefulActor _testActorOne;

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
            _eventStoreBus.Dispose();
            _testActorOne.Dispose();
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

        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, AllStreamsCatchupCache<SomeDataAggregate> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate> someDataAggregates) CreateCatchupEventStoreCache()
        {
            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

            var connectionMonitor = new EventStoreConnectionStatusMonitor(connection, _loggerFactory);

            var cacheConfiguration = new AllStreamsCatchupCacheConfiguration(Position.Start)
            {
                UserCredentials = _userCredentials,
                KeepAppliedEventsOnAggregate = true,
                IsStaleTimeSpan = TimeSpan.FromSeconds(1)
            };

            var catchUpCache = new AllStreamsCatchupCache<SomeDataAggregate>(
              connectionMonitor,
              cacheConfiguration,
             new DefaultEventTypeProvider<SomeDataAggregate>(() => new[] { typeof(SomeDataAggregateEvent) }),
             _loggerFactory);

            var aggregatesOnCacheOne = new ObservableCollectionExtended<SomeDataAggregate>();

            catchUpCache.AsObservableCache()
                           .Connect()
                           .Bind(aggregatesOnCacheOne)
                           .Subscribe();

            return (connectionMonitor, catchUpCache, aggregatesOnCacheOne);

        }

        [Test, Order(0)]
        public async Task ShouldCreateAnActor()
        {
            _eventRepository = CreateEventRepository();
            _cacheOne = CreateCatchupEventStoreCache();
            _eventStoreBus = new EventStoreBus(_eventRepository.connectionStatusMonitor, _eventRepository.eventStoreRepository);

            await Task.Delay(100);

            var catchupEventStoreCacheConfiguration = new AllStreamsCatchupCacheConfiguration(Position.Start)
            {
                KeepAppliedEventsOnAggregate = true
            };

            var defaultEventTypeProvider = new DefaultEventTypeProvider<SomeDataAggregate>(() => new[] { typeof(SomeDataAggregateEvent) });

            _testActorOne = new TestStatefulActor(ActorConfiguration.Default, _cacheOne.connectionStatusMonitor,
                catchupEventStoreCacheConfiguration, defaultEventTypeProvider, new DummyLoggerFactory());

            await _testActorOne.ConnectTo(_eventStoreBus);

            _testActorOne.SubscribeToAllStreams(Position.Start);

            await _testActorOne.ConnectToEventStream();

            Assert.NotNull(_testActorOne);
        }

        [Test, Order(1)]
        public async Task ShouldEmitEventsAndUpdateCache()
        {

            await _testActorOne.EmitEventStore(new SomeDataAggregateEvent($"{_firstAggregateId}", Guid.NewGuid()));

            await Task.Delay(500);

            var current = _testActorOne.GetCurrent($"{_firstAggregateId}");

            Assert.NotNull(current);
            Assert.AreEqual(1, current.AppliedEvents.Length);

            await _testActorOne.EmitEventStore(new SomeRandomEvent(Guid.NewGuid()));

            await Task.Delay(500);

            Assert.AreEqual(1, _testActorOne.Events.Count);

            _testActorOne.Dispose();

        }
    }
}

