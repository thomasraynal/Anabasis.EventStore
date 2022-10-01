using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
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
using Anabasis.EventStore.Stream;
using DynamicData;
using Anabasis.EventStore.Factories;
using Anabasis.EventStore.Snapshot;

namespace Anabasis.EventStore.Tests
{
    public class TestErrorHandlingStatefulActor : SubscribeToAllStreamsEventStoreStatefulActor<SomeDataAggregate>
    {
        public TestErrorHandlingStatefulActor(IActorConfiguration actorConfiguration, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, AllStreamsCatchupCacheConfiguration<SomeDataAggregate> catchupCacheConfiguration, IEventTypeProvider<SomeDataAggregate> eventTypeProvider, ILoggerFactory loggerFactory, ISnapshotStore<SomeDataAggregate> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null) : base(actorConfiguration, connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy)
        {
        }

        public TestErrorHandlingStatefulActor(IEventStoreActorConfigurationFactory eventStoreActorConfigurationFactory, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, AllStreamsCatchupCacheConfiguration<SomeDataAggregate> catchupCacheConfiguration, IEventTypeProvider<SomeDataAggregate> eventTypeProvider, ILoggerFactory loggerFactory, ISnapshotStore<SomeDataAggregate> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, IKillSwitch killSwitch = null) : base(eventStoreActorConfigurationFactory, connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy, killSwitch)
        {
        }

        public List<SomeRandomEvent> Events { get; } = new List<SomeRandomEvent>();

        public Task Handle(SomeRandomEvent someMoreData)
        {
            throw new Exception("boom");
        }

    }

    public class SomeFaultyAggregateEvent : BaseAggregateEvent<SomeDataAggregate>
    {

        public static bool ShouldFail = true;

        public SomeFaultyAggregateEvent(string entityId, Guid correlationId) : base(entityId, correlationId)
        {
        }

        public override void Apply(SomeDataAggregate entity)
        {
            if (ShouldFail)
                throw new Exception("boom");
        }
    }

    [TestFixture]
    public class TestErrorHandling
    {

        private UserCredentials _userCredentials;
        private ConnectionSettings _connectionSettings;
        private LoggerFactory _loggerFactory;
        private ClusterVNode _clusterVNode;

        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, AllStreamsCatchupCache<SomeDataAggregate> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate> someDataAggregates) _cacheOne;
        private EventStoreBus _eventStoreBus;
        private Guid _firstAggregateId = Guid.NewGuid();

        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository eventStoreRepository) _eventRepository;
        private TestErrorHandlingStatefulActor _testActorOne;

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

            var cacheConfiguration = new AllStreamsCatchupCacheConfiguration<SomeDataAggregate>(Position.Start)
            {
                UserCredentials = _userCredentials,
                KeepAppliedEventsOnAggregate = true,
                IsStaleTimeSpan = TimeSpan.FromSeconds(1)
            };

            var catchUpCache = new AllStreamsCatchupCache<SomeDataAggregate>(
              connectionMonitor,
              cacheConfiguration,
             new DefaultEventTypeProvider<SomeDataAggregate>(() => new[] { typeof(SomeFaultyAggregateEvent) }),
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

            var allStreamsCatchupCacheConfiguration = new AllStreamsCatchupCacheConfiguration<SomeDataAggregate>(Position.Start)
            {
              KeepAppliedEventsOnAggregate = true

            };

            var defaultEventTypeProvider = new DefaultEventTypeProvider<SomeDataAggregate>(() => new[] { typeof(SomeFaultyAggregateEvent) });

            _testActorOne = new TestErrorHandlingStatefulActor(ActorConfiguration.Default, _cacheOne.connectionStatusMonitor,
                allStreamsCatchupCacheConfiguration, defaultEventTypeProvider, new DummyLoggerFactory());

            await _testActorOne.ConnectTo(_eventStoreBus);

            await _testActorOne.ConnectToEventStream();

            Assert.NotNull(_testActorOne);
        }

        [Test, Order(1)]
        public async Task ShouldEmitAnAggregateEventAndFailThenSucceedAndUpdateCache()
        {
            SomeFaultyAggregateEvent.ShouldFail = true;

            await _testActorOne.EmitEventStore(new SomeFaultyAggregateEvent($"{_firstAggregateId}", Guid.NewGuid()));

            await Task.Delay(500);

            var current = _testActorOne.GetCurrent($"{_firstAggregateId}");

            Assert.Null(current);

            SomeFaultyAggregateEvent.ShouldFail = false;

            await Task.Delay(1000);

            current = _testActorOne.GetCurrent($"{_firstAggregateId}");

            Assert.NotNull(current);

            Assert.AreEqual(1, current.AppliedEvents.Length);

        }

        //[Test, Order(2)]
        //public async Task ShouldEmitAnEventAndFailToConsumeIt()
        //{
        //    await _testActorOne.EmitEventStore(new SomeRandomEvent(Guid.NewGuid()));

        //    await Task.Delay(3000);
        //}
    }
}