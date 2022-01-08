using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Event;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Stream;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Tests.Components;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using EventStore.Core;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Tests
{
    public class SomeCommandResponse : BaseCommandResponse
    {
        public SomeCommandResponse(Guid commandId, Guid correlationId, string streamId) : base(commandId, correlationId, streamId)
        {
        }
    }

    public class SomeCommand : BaseCommand
    {

        public SomeCommand(Guid correlationId, string streamId) : base(correlationId, streamId)
        {
        }
    }

    public class TestActorReceiver : BaseStatelessActor
    {
        public TestActorReceiver(IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, loggerFactory)
        {

        }

        public async Task Handle(SomeCommand someCommand)
        {
            await EmitEventStore(new SomeCommandResponse(someCommand.EventID, someCommand.CorrelationID, someCommand.EntityId));
        }

        public override void Dispose()
        {
            base.Dispose();
        }

    }

    public class TestActor : BaseStatelessActor
    {
        public TestActor(IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, loggerFactory)
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
    public class TestActors
    {

        private UserCredentials _userCredentials;
        private ConnectionSettings _connectionSettings;
        private ClusterVNode _clusterVNode;
        private (ConnectionStatusMonitor connectionStatusMonitor, IEventStoreRepository eventStoreRepository) _eventRepository;
        private TestActor _testActorOne;
        private (ConnectionStatusMonitor connectionStatusMonitor, PersistentSubscriptionEventStoreStream persistentEventStoreStream) _streamOne;
        private TestActor _testActorTwo;
        private (ConnectionStatusMonitor connectionStatusMonitor, PersistentSubscriptionEventStoreStream persistentEventStoreStream) _streamTwo;

        private Guid _correlationId = Guid.NewGuid();
        private readonly string _streamId = "streamId";
        private readonly string _groupIdOne = "groupIdOne";
        private readonly string _groupIdTwo = "groupIdTwo";
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

            await CreateSubscriptionGroups();
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            await _clusterVNode.StopAsync();
        }

        private async Task CreateSubscriptionGroups()
        {
            var connectionSettings = PersistentSubscriptionSettings.Create().StartFromCurrent().Build();
            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode);

            await connection.CreatePersistentSubscriptionAsync(
                 _streamId,
                 _groupIdOne,
                 connectionSettings,
                 _userCredentials);

            await connection.CreatePersistentSubscriptionAsync(
                 _streamId,
                 _groupIdTwo,
                 connectionSettings,
                 _userCredentials);
        }

        private (ConnectionStatusMonitor connectionStatusMonitor, SubscribeFromEndEventStoreStream volatileEventStoreStream) CreateVolatileEventStoreStream()
        {
            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

            var connectionMonitor = new ConnectionStatusMonitor(connection, _loggerFactory);

            var volatileEventStoreStreamConfiguration = new SubscribeFromEndEventStoreStreamConfiguration(_userCredentials);

            var volatileEventStoreStream = new SubscribeFromEndEventStoreStream(
              connectionMonitor,
              volatileEventStoreStreamConfiguration,
              new DefaultEventTypeProvider(() => new[] { typeof(SomeRandomEvent), typeof(SomeCommandResponse), typeof(SomeCommand) }),
              _loggerFactory);

            return (connectionMonitor, volatileEventStoreStream);

        }

        private (ConnectionStatusMonitor connectionStatusMonitor, IEventStoreRepository eventStoreRepository) CreateEventRepository()
        {
            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();
            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
            var connectionMonitor = new ConnectionStatusMonitor(connection, _loggerFactory);

            var eventStoreRepository = new EventStoreRepository(
              eventStoreRepositoryConfiguration,
              connection,
              connectionMonitor,
              _loggerFactory);

            return (connectionMonitor, eventStoreRepository);
        }

        private (ConnectionStatusMonitor connectionStatusMonitor, PersistentSubscriptionEventStoreStream persistentEventStoreStream) CreatePersistentEventStoreStream(string streamId, string groupId)
        {
            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);

            var connectionMonitor = new ConnectionStatusMonitor(connection, _loggerFactory);

            var persistentEventStoreStreamConfiguration = new PersistentSubscriptionEventStoreStreamConfiguration(streamId, groupId, _userCredentials);

            var persistentSubscriptionEventStoreStream = new PersistentSubscriptionEventStoreStream(
              connectionMonitor,
              persistentEventStoreStreamConfiguration,
              new DefaultEventTypeProvider(() => new[] { typeof(SomeRandomEvent) }),
              _loggerFactory);

            return (connectionMonitor, persistentSubscriptionEventStoreStream);

        }

        [Test, Order(0)]
        public async Task ShouldCreateAnActor()
        {
            _eventRepository = CreateEventRepository();

            await Task.Delay(100);

            _testActorOne = new TestActor(_eventRepository.eventStoreRepository, _loggerFactory);

            Assert.NotNull(_testActorOne);

        }

        [Test, Order(1)]
        public async Task ShouldCreateAStreamAndBindItToTheActor()
        {
            _eventRepository = CreateEventRepository();

            await Task.Delay(100);

            _testActorOne = new TestActor(_eventRepository.eventStoreRepository, _loggerFactory);

            Assert.NotNull(_testActorOne);

            _streamOne = CreatePersistentEventStoreStream(_streamId, _groupIdOne);

            _testActorOne.SubscribeToEventStream(_streamOne.persistentEventStoreStream);

            await _testActorOne.EmitEventStore(new SomeRandomEvent(_correlationId, _streamId));

            await Task.Delay(100);

            Assert.AreEqual(1, _testActorOne.Events.Count);

        }

        [Test, Order(2)]
        public async Task ShouldCreateASecondAndLoadBalanceEvents()
        {

            _testActorTwo = new TestActor(_eventRepository.eventStoreRepository, _loggerFactory);

            Assert.NotNull(_testActorOne);

            _streamTwo = CreatePersistentEventStoreStream(_streamId, _groupIdOne);

            _testActorTwo.SubscribeToEventStream(_streamTwo.persistentEventStoreStream);

            var events = Enumerable.Range(0, 10).Select(_ => new SomeRandomEvent(_correlationId, _streamId)).ToArray();

            foreach (var ev in events)
            {
                await _eventRepository.eventStoreRepository.Emit(ev);
            }

            await Task.Delay(100);

            Assert.True(_testActorOne.Events.Count > 1);
            Assert.True(_testActorTwo.Events.Count > 1);

            var consumedEvents = _testActorOne.Events.Concat(_testActorTwo.Events).ToArray();

            Assert.True(events.All(ev => consumedEvents.Any(e => e.EventID == ev.EventID)));

        }

        [Test, Order(3)]
        public async Task ShouldSendACommand()
        {
            var (_, volatileEventStoreStream) = CreateVolatileEventStoreStream();

            var sender = new TestActor(_eventRepository.eventStoreRepository, _loggerFactory);
            sender.SubscribeToEventStream(volatileEventStoreStream);

            var receiver = new TestActorReceiver(_eventRepository.eventStoreRepository, _loggerFactory);
            receiver.SubscribeToEventStream(volatileEventStoreStream);

            var someCommandResponse = await sender.SendEventStore<SomeCommandResponse>(new SomeCommand(Guid.NewGuid(), "some-stream"), TimeSpan.FromSeconds(3));

            Assert.NotNull(someCommandResponse);
        }

    }
}
