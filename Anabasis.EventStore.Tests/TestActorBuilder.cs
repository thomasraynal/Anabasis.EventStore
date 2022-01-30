using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Stream;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Standalone;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using EventStore.Core;
using Lamar;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Anabasis.Common;

namespace Anabasis.EventStore.Tests
{
    public class SomeDependency : ISomeDependency
    {

    }

    public interface ISomeDependency
    {
    }

    public class SomeRegistry : ServiceRegistry
    {
        public SomeRegistry()
        {
            For<ISomeDependency>().Use<SomeDependency>();
        }
    }

    public class TestActorAutoBuildOne : BaseEventStoreStatelessActor
    {
        public List<IEvent> Events { get; } = new List<IEvent>();

        public TestActorAutoBuildOne(IActorConfiguration actorConfiguration, IEventStoreRepository eventStoreRepository, ISomeDependency _, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, loggerFactory)
        {
        }

        public Task Handle(AgainSomeMoreData againSomeMoreData)
        {
            Events.Add(againSomeMoreData);

            return Task.CompletedTask;
        }

        public Task Handle(SomeMoreData someMoreData)
        {
            Events.Add(someMoreData);

            return Task.CompletedTask;
        }

    }

    public class TestActorAutoBuildTwo : BaseEventStoreStatelessActor
    {

        public List<IEvent> Events { get; } = new List<IEvent>();

        public TestActorAutoBuildTwo(IActorConfiguration actorConfiguration, IEventStoreRepository eventStoreRepository, ISomeDependency _, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, loggerFactory)
        {
        }
        public async Task Handle(SomeCommand someCommand)
        {
            await EmitEventStore(new SomeCommandResponse(someCommand.EventID, someCommand.CorrelationID, someCommand.EntityId));
        }

        public Task Handle(AgainSomeMoreData againSomeMoreData)
        {
            Events.Add(againSomeMoreData);

            return Task.CompletedTask;
        }

        public Task Handle(SomeMoreData someMoreData)
        {
            Events.Add(someMoreData);

            return Task.CompletedTask;
        }
    }


    [TestFixture]
    public class TestActorBuilder
    {

        private UserCredentials _userCredentials;
        private ConnectionSettings _connectionSettings;
        private ClusterVNode _clusterVNode;
        private ILoggerFactory _loggerFactory;

        private Guid _correlationId = Guid.NewGuid();
        private readonly string _streamId = "streamId";
        private readonly string _streamId2 = "streamId2";
        private readonly string _groupIdOne = "groupIdOne";
        private readonly string _groupIdTwo = "groupIdTwo";

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

            await connection.CreatePersistentSubscriptionAsync(
                 _streamId2,
                 _groupIdOne,
                 connectionSettings,
                 _userCredentials);
        }


        [Test, Order(0)]
        public async Task ShouldBuildAndRunActors()
        {

            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
            var connectionMonitor = new ConnectionStatusMonitor(connection, _loggerFactory);

            var eventProvider = new ConsumerBasedEventProvider<TestActorAutoBuildOne>();

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();

            var actorConfiguration = new ActorConfiguration();

            var eventStoreRepository = new EventStoreRepository(
              eventStoreRepositoryConfiguration,
              connection,
              connectionMonitor,
              _loggerFactory);

            var persistentEventStoreStreamConfiguration = new PersistentSubscriptionEventStoreStreamConfiguration(_streamId, _groupIdOne, _userCredentials);

            var persistentSubscriptionEventStoreStream = new PersistentSubscriptionEventStoreStream(
              connectionMonitor,
              persistentEventStoreStreamConfiguration,
              eventProvider,
              _loggerFactory);

            var volatileEventStoreStreamConfiguration = new SubscribeFromEndEventStoreStreamConfiguration(_userCredentials);

            var volatileEventStoreStream = new SubscribeFromEndEventStoreStream(
              connectionMonitor,
              volatileEventStoreStreamConfiguration,
              eventProvider,
              _loggerFactory);

            await Task.Delay(1000);

            var testActorAutoBuildOne = new TestActorAutoBuildOne(actorConfiguration, eventStoreRepository, new SomeDependency(), _loggerFactory);
            var testActorAutoBuildTwo = new TestActorAutoBuildOne(actorConfiguration, eventStoreRepository, new SomeDependency(), _loggerFactory);

            testActorAutoBuildOne.SubscribeToEventStream(persistentSubscriptionEventStoreStream);
            testActorAutoBuildOne.SubscribeToEventStream(volatileEventStoreStream);

            await Task.Delay(2000);

            await testActorAutoBuildTwo.EmitEventStore(new SomeMoreData(_correlationId, "some-stream"));

            await Task.Delay(1000);

            Assert.AreEqual(1, testActorAutoBuildOne.Events.Count);

            await testActorAutoBuildTwo.EmitEventStore(new SomeMoreData(_correlationId, _streamId));

            await Task.Delay(100);

            Assert.AreEqual(3, testActorAutoBuildOne.Events.Count);
        }

        [Ignore("Non deterministic")]
        [Test, Order(1)]
        public async Task ShouldBuildFromActorBuilderAndRunActors()
        {

            var testActorAutoBuildOne = StatelessActorBuilder<TestActorAutoBuildOne, SomeRegistry>.Create(_clusterVNode, _connectionSettings, ActorConfiguration.Default, _loggerFactory)
                                                                                         .WithSubscribeFromEndToAllStream()
                                                                                         .WithPersistentSubscriptionStream(_streamId2, _groupIdOne)
                                                                                         .Build();

            var testActorAutoBuildTwo = StatelessActorBuilder<TestActorAutoBuildOne, SomeRegistry>.Create(_clusterVNode, _connectionSettings, ActorConfiguration.Default, _loggerFactory)
                                                                                         .Build();


            await Task.Delay(1000);

            await testActorAutoBuildTwo.EmitEventStore(new SomeMoreData(_correlationId, "some-stream"));
                
            await Task.Delay(2000);

            Assert.AreEqual(1, testActorAutoBuildOne.Events.Count);

            await testActorAutoBuildTwo.EmitEventStore(new SomeMoreData(_correlationId, _streamId2));

            await Task.Delay(1000);

            Assert.AreEqual(3, testActorAutoBuildOne.Events.Count);
        }

    }
}
