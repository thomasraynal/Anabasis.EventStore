using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Stream;
using Anabasis.EventStore.Repository;
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
    public class TestStatefulActor : BaseEventStoreStatefulActor<SomeDataAggregate>
    {
        public TestStatefulActor(AllStreamsCatchupCache<SomeDataAggregate> catchupEventStoreCache,
          IEventStoreAggregateRepository eventStoreRepository) : base(eventStoreRepository, catchupEventStoreCache)
        {
            Events = new List<SomeRandomEvent>();
        }

        public List<SomeRandomEvent> Events { get; }

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

        private (ConnectionStatusMonitor connectionStatusMonitor,AllStreamsCatchupCache<SomeDataAggregate> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate> someDataAggregates) _cacheOne;

        private Guid _firstAggregateId = Guid.NewGuid();

        private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository eventStoreRepository) _eventRepository;
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
            await _clusterVNode.StopAsync();
        }

        private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository eventStoreRepository) CreateEventRepository()
        {
            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();
            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
            var connectionMonitor = new ConnectionStatusMonitor(connection, _loggerFactory);

            var eventStoreRepository = new EventStoreAggregateRepository(
              eventStoreRepositoryConfiguration,
              connection,
              connectionMonitor,
              _loggerFactory);

            return (connectionMonitor, eventStoreRepository);
        }

        private (ConnectionStatusMonitor connectionStatusMonitor,AllStreamsCatchupCache<SomeDataAggregate> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate> someDataAggregates) CreateCatchupEventStoreCache()
        {
            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

            var connectionMonitor = new ConnectionStatusMonitor(connection, _loggerFactory);

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
            _eventRepository = CreateEventRepository();
            _cacheOne = CreateCatchupEventStoreCache();

            await Task.Delay(100);

            var subscribeToAllStream = new SubscribeFromEndEventStoreStream(_cacheOne.connectionStatusMonitor,
                new SubscribeFromEndEventStoreStreamConfiguration(),
                new ConsumerBasedEventProvider<TestStatefulActor>(),
                _loggerFactory);

            _testActorOne = new TestStatefulActor(_cacheOne.catchupEventStoreCache, _eventRepository.eventStoreRepository);

            _testActorOne.SubscribeToEventStream(subscribeToAllStream);

            Assert.NotNull(_testActorOne);
        }

        [Test, Order(1)]
        public async Task ShouldEmitEventsAndUpdateCache()
        {

            await _testActorOne.EmitEventStore(new SomeData($"{_firstAggregateId}", Guid.NewGuid()));

            await Task.Delay(500);

            var current = _testActorOne.State.GetCurrent($"{_firstAggregateId}");

            Assert.NotNull(current);
            Assert.AreEqual(1, current.AppliedEvents.Length);

            await _testActorOne.EmitEventStore(new SomeRandomEvent(Guid.NewGuid()));

            await Task.Delay(500);

            Assert.AreEqual(1, _testActorOne.Events.Count);

        }
    }
}

