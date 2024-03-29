using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Stream;
using Anabasis.EventStore.Repository;
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
using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.EventStore.Standalone;

[assembly: NonParallelizable]

namespace Anabasis.EventStore.Tests
{
    public class SomeCommandResponse : BaseCommandResponse
    {
        public SomeCommandResponse(Guid commandId, Guid correlationId, string streamId) : base(streamId, commandId, correlationId)
        {
        }
    }

    public class SomeCommandResponse2 : BaseCommandResponse
    {
        public SomeCommandResponse2(Guid commandId, Guid correlationId, string streamId) : base(streamId, commandId, correlationId)
        {
        }
    }

    public class SomeCommand : BaseCommand
    {

        public SomeCommand(Guid correlationId, string streamId) : base(streamId, correlationId)
        {
        }
    }

    public class SomeCommand2 : BaseCommand
    {

        public SomeCommand2(Guid correlationId, string streamId) : base(streamId, correlationId)
        {
        }
    }

    public class TestActorReceiver : BaseStatelessActor
    {
        public TestActorReceiver(IActorConfigurationFactory actorConfigurationFactory, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, loggerFactory)
        {
        }

        public TestActorReceiver(IActorConfiguration actorConfiguration, ILoggerFactory loggerFactory = null) : base(actorConfiguration, loggerFactory)
        {
        }

        public async Task Handle(SomeCommand2 someCommand)
        {
            await this.EmitEventStore(new SomeCommandResponse2(someCommand.EventId, someCommand.CorrelationId, someCommand.EntityId));
        }

        public override void Dispose()
        {
            base.Dispose();
        }

    }

    public class TestActor : BaseStatelessActor
    {
        public TestActor(IActorConfigurationFactory actorConfigurationFactory, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, loggerFactory)
        {
        }

        public TestActor(IActorConfiguration actorConfiguration, ILoggerFactory loggerFactory = null) : base(actorConfiguration, loggerFactory)
        {
        }

        public List<SomeRandomEvent> Events { get; } = new List<SomeRandomEvent>();

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

        private IActorConfiguration _actorConfiguration = new ActorConfiguration();

        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, IEventStoreRepository eventStoreRepository) _eventRepository;
        private TestActor _testActorOne;
        private EventStoreBus _eventStoreBus;
       private TestActor _testActorTwo;
     
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
            _eventStoreBus.Dispose();
            _testActorOne.Dispose();
            _testActorTwo.Dispose();

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

        private (EventStoreConnectionStatusMonitor connectionStatusMonitor, IEventStoreRepository eventStoreRepository) CreateEventRepository()
        {
            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();
            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
            var connectionMonitor = new EventStoreConnectionStatusMonitor(connection, _loggerFactory);

            var eventStoreRepository = new EventStoreRepository(
              eventStoreRepositoryConfiguration,
              connection,
              connectionMonitor,
              _loggerFactory);

            return (connectionMonitor, eventStoreRepository);
        }

        [Test, Order(0)]
        public async Task ShouldCreateAnActor()
        {
            _eventRepository = CreateEventRepository();

            await Task.Delay(100);

            _testActorOne = new TestActor(_actorConfiguration, _loggerFactory);
            _eventStoreBus = new EventStoreBus(_eventRepository.connectionStatusMonitor, _eventRepository.eventStoreRepository);

            await _testActorOne.ConnectTo(_eventStoreBus);

            Assert.NotNull(_testActorOne);

        }

        [Test, Order(1)]
        public async Task ShouldCreateAStreamAndBindItToTheActor()
        {
            _eventRepository = CreateEventRepository();

            await Task.Delay(100);

            _testActorOne = new TestActor(_actorConfiguration, _loggerFactory);

            await _testActorOne.ConnectTo(_eventStoreBus);

            Assert.NotNull(_testActorOne);

            _testActorOne.SubscribeToPersistentSubscriptionStream(_streamId, _groupIdOne);

            await _testActorOne.EmitEventStore(new SomeRandomEvent(_correlationId, _streamId));

            await Task.Delay(100);

            Assert.AreEqual(1, _testActorOne.Events.Count);

        }

        [Test, Order(2)]
        public async Task ShouldCreateASecondAndLoadBalanceEvents()
        {

            _testActorTwo = new TestActor(_actorConfiguration, _loggerFactory);

            await _testActorTwo.ConnectTo(_eventStoreBus);

            Assert.NotNull(_testActorOne);

            _testActorTwo.SubscribeToPersistentSubscriptionStream(_streamId, _groupIdOne);

            var events = Enumerable.Range(0, 10).Select(_ => new SomeRandomEvent(_correlationId, _streamId)).ToArray();

            foreach (var ev in events)
            {
                await _eventRepository.eventStoreRepository.Emit(ev);
            }

            await Task.Delay(100);

            Assert.True(_testActorOne.Events.Count > 1);
            Assert.True(_testActorTwo.Events.Count > 1);

            var consumedEvents = _testActorOne.Events.Concat(_testActorTwo.Events).ToArray();

            Assert.True(events.All(ev => consumedEvents.Any(e => e.EventId == ev.EventId)));

        }

        //[Test, Order(3)]
        //public async Task ShouldSendACommand()
        //{
        //    var (_, volatileEventStoreStream) = CreateVolatileEventStoreStream();

        //    await Task.Delay(500);

        //    var sender = new TestActor(_actorConfiguration, _loggerFactory);
        //    await sender.ConnectTo(_eventStoreBus);

        //    sender.SubscribeToEventStream(volatileEventStoreStream);

        //    var receiver = new TestActorReceiver(_actorConfiguration, _loggerFactory);
        //    await receiver.ConnectTo(_eventStoreBus);

        //    receiver.SubscribeToEventStream(volatileEventStoreStream);

        //    await Task.Delay(2000);

        //    var someCommandResponse = await sender.SendEventStore<SomeCommandResponse2>(new SomeCommand2(Guid.NewGuid(), "some-other-stream"), TimeSpan.FromSeconds(5));

        //    Assert.NotNull(someCommandResponse);

        //    sender.Dispose();
        //    receiver.Dispose();

        //}

    }
}
