using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Queue;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Shared;
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

    public class TestActorAutoBuildOne : BaseStatelessActor
    {
        public List<IEvent> Events { get; } = new List<IEvent>();

        public TestActorAutoBuildOne(IEventStoreRepository eventStoreRepository, ISomeDependency _, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, loggerFactory)
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

    public class TestActorAutoBuildTwo : BaseStatelessActor
    {

        public List<IEvent> Events { get; } = new List<IEvent>();

        public TestActorAutoBuildTwo(IEventStoreRepository eventStoreRepository, ISomeDependency _, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, loggerFactory)
        {
        }
        public async Task Handle(SomeCommand someCommand)
        {
            await Emit(new SomeCommandResponse(someCommand.EventID, someCommand.CorrelationID, someCommand.StreamId));
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

            var eventStoreRepository = new EventStoreRepository(
              eventStoreRepositoryConfiguration,
              connection,
              connectionMonitor,
              _loggerFactory);

            var persistentEventStoreQueueConfiguration = new PersistentSubscriptionEventStoreQueueConfiguration(_streamId, _groupIdOne, _userCredentials);

            var persistentSubscriptionEventStoreQueue = new PersistentSubscriptionEventStoreQueue(
              connectionMonitor,
              persistentEventStoreQueueConfiguration,
              eventProvider,
              _loggerFactory);

            var volatileEventStoreQueueConfiguration = new SubscribeFromEndEventStoreQueueConfiguration(_userCredentials);

            var volatileEventStoreQueue = new SubscribeFromEndEventStoreQueue(
              connectionMonitor,
              volatileEventStoreQueueConfiguration,
              eventProvider,
              _loggerFactory);

            var testActorAutoBuildOne = new TestActorAutoBuildOne(eventStoreRepository, new SomeDependency(), _loggerFactory);
            var testActorAutoBuildTwo = new TestActorAutoBuildOne(eventStoreRepository, new SomeDependency(), _loggerFactory);

            testActorAutoBuildOne.SubscribeTo(persistentSubscriptionEventStoreQueue);
            testActorAutoBuildOne.SubscribeTo(volatileEventStoreQueue);

            await testActorAutoBuildTwo.Emit(new SomeMoreData(_correlationId, "some-stream"));

            await Task.Delay(100);

            Assert.AreEqual(1, testActorAutoBuildOne.Events.Count);

            await testActorAutoBuildTwo.Emit(new SomeMoreData(_correlationId, _streamId));

            await Task.Delay(100);

            Assert.AreEqual(3, testActorAutoBuildOne.Events.Count);
        }

        [Test, Order(1)]
        public async Task ShouldBuildFromActorBuilderAndRunActors()
        {

            var testActorAutoBuildOne = StatelessActorBuilder<TestActorAutoBuildOne, SomeRegistry>.Create(_clusterVNode, _connectionSettings, _loggerFactory)
                                                                                         .WithSubscribeFromEndToAllQueue()
                                                                                         .WithPersistentSubscriptionQueue(_streamId2, _groupIdOne)
                                                                                         .Build();

            var testActorAutoBuildTwo = StatelessActorBuilder<TestActorAutoBuildOne, SomeRegistry>.Create(_clusterVNode, _connectionSettings, _loggerFactory)
                                                                                         .Build();

            await testActorAutoBuildTwo.Emit(new SomeMoreData(_correlationId, "some-stream"));

            await Task.Delay(500);

            Assert.AreEqual(1, testActorAutoBuildOne.Events.Count);

            await testActorAutoBuildTwo.Emit(new SomeMoreData(_correlationId, _streamId2));

            await Task.Delay(500);

            Assert.AreEqual(3, testActorAutoBuildOne.Events.Count);
        }

    }
}
