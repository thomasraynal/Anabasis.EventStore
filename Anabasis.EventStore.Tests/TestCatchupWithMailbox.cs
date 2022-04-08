using Anabasis.EventStore.Actor;
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
using Anabasis.EventStore.Factories;
using Anabasis.EventStore.Snapshot;

namespace Anabasis.EventStore.Tests
{

    public class TestCatchupWithMailboxActor : BaseEventStoreStatefulActor<SomeDataAggregate>
    {
        public TestCatchupWithMailboxActor(IActorConfiguration actorConfiguration, IAggregateCache<SomeDataAggregate> eventStoreCache, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreCache, loggerFactory)
        {
        }

        public TestCatchupWithMailboxActor(IEventStoreActorConfigurationFactory eventStoreCacheFactory, IConnectionStatusMonitor<IEventStoreConnection> connectionStatusMonitor, ISnapshotStore<SomeDataAggregate> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, ILoggerFactory loggerFactory = null) : base(eventStoreCacheFactory, connectionStatusMonitor, snapshotStore, snapshotStrategy, loggerFactory)
        {
        }

        public List<SomeData> Events { get; } = new List<SomeData>();

        public Task Handle(SomeData someData)
        {
            Events.Add(someData);

            return Task.CompletedTask;
        }
    }

    [TestFixture]
    public class TestCatchupWithMailbox
    {

        private UserCredentials _userCredentials;
        private ConnectionSettings _connectionSettings;
        private LoggerFactory _loggerFactory;
        private ClusterVNode _clusterVNode;

        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, AllStreamsCatchupCache<SomeDataAggregate> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate> someDataAggregates) _cacheOne;
        private EventStoreBus _eventStoreBus;
        private DummyBus _dummyBus;
        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository eventStoreRepository) _eventRepository;
        private TestCatchupWithMailboxActor _testActorOne;

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
             new DefaultEventTypeProvider<SomeDataAggregate>(() => new[] { typeof(SomeData) }),
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
            _dummyBus = new DummyBus();
            _eventRepository = CreateEventRepository();

            _cacheOne = CreateCatchupEventStoreCache();
            _eventStoreBus = new EventStoreBus(_eventRepository.connectionStatusMonitor, _eventRepository.eventStoreRepository);

            await Task.Delay(100);

            _testActorOne = new TestCatchupWithMailboxActor(ActorConfiguration.Default, _cacheOne.catchupEventStoreCache, new DummyLoggerFactory());

            await _testActorOne.ConnectTo(_eventStoreBus);
            await _testActorOne.ConnectTo(_dummyBus);

            _testActorOne.SubscribeDummyBus("somesubject");
            await Task.Delay(100);

            Assert.NotNull(_testActorOne);
        }

        [Test, Order(1)]
        public async Task ShouldDisconnectAndHoldMessageConsumptionUntilCatchup()
        {

            Assert.True(_testActorOne.IsCaughtUp);

            await _cacheOne.catchupEventStoreCache.Disconnect();

            await Task.Delay(100);

            Assert.False(_testActorOne.IsCaughtUp);

            _ = Task.Run(() =>
              {
                  _dummyBus.Push(new SomeData("entityId", Guid.NewGuid()));
              });

       
            await Task.Delay(100);

            Assert.AreEqual(0, _testActorOne.Events.Count);

            await _cacheOne.catchupEventStoreCache.Connect();

            await Task.Delay(100);

            Assert.True(_testActorOne.IsCaughtUp);

            Assert.AreEqual(1, _testActorOne.Events.Count);

            _testActorOne.Dispose();

        }
    }
    
}
