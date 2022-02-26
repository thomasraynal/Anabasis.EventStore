using Anabasis.EventStore.Actor;
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
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Stream;
using DynamicData;
using Microsoft.Reactive.Testing;
using System.Reactive.Concurrency;

namespace Anabasis.EventStore.Tests
{
    public class TestErrorHandlingStatefulActor : BaseEventStoreStatefulActor<SomeDataAggregate>
    {
        public TestErrorHandlingStatefulActor(IActorConfiguration actorConfiguration, AllStreamsCatchupCache<SomeDataAggregate> catchupEventStoreCache,
          IEventStoreAggregateRepository eventStoreRepository) : base(actorConfiguration, eventStoreRepository, catchupEventStoreCache)
        {
            Events = new List<SomeRandomEvent>();
        }

        public List<SomeRandomEvent> Events { get; }

        public Task Handle(SomeRandomEvent someMoreData)
        {
            throw new Exception("boom");
        }

    }

    public class SomeFaultyData : BaseAggregateEvent<SomeDataAggregate>
    {

        public SomeFaultyData(string entityId, Guid correlationId) : base(entityId, correlationId)
        {
        }

        public override void Apply(SomeDataAggregate entity)
        {
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

        private (ConnectionStatusMonitor connectionStatusMonitor, AllStreamsCatchupCache<SomeDataAggregate> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate> someDataAggregates) _cacheOne;

        private Guid _firstAggregateId = Guid.NewGuid();

        private (ConnectionStatusMonitor connectionStatusMonitor, EventStoreAggregateRepository eventStoreRepository) _eventRepository;
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
            _testActorOne.Dispose();
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

        private (ConnectionStatusMonitor connectionStatusMonitor, AllStreamsCatchupCache<SomeDataAggregate> catchupEventStoreCache, ObservableCollectionExtended<SomeDataAggregate> someDataAggregates) CreateCatchupEventStoreCache()
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
             new DefaultEventTypeProvider<SomeDataAggregate>(() => new[] { typeof(SomeFaultyData) }),
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

            _testActorOne = new TestErrorHandlingStatefulActor(ActorConfiguration.Default, _cacheOne.catchupEventStoreCache, _eventRepository.eventStoreRepository);

            _testActorOne.SubscribeToEventStream(subscribeToAllStream);

            

            Assert.NotNull(_testActorOne);
        }

        [Test, Order(1)]
        public async Task ShouldEmitEventsAndUpdateCache()
        {
            //var exp = Assert.ThrowsAsync<Exception>(async () =>
            //{
            //try
            //{
            //  await  Task.Run(async() =>
            //     {
            //         await _testActorOne.EmitEventStore(new SomeFaultyData($"{_firstAggregateId}", Guid.NewGuid()));



            //         var current = _testActorOne.State.GetCurrent($"{_firstAggregateId}");

            //     });

            //}
            //catch(Exception ex)
            //{

            //}

            //});


            //Assert.NotNull(current);
            //Assert.AreEqual(1, current.AppliedEvents.Length);

            //await _testActorOne.EmitEventStore(new SomeRandomEvent(Guid.NewGuid()));

            await Task.Delay(5000);

            //Assert.AreEqual(1, _testActorOne.Events.Count);

            //_testActorOne.Dispose();

        }
    }
}