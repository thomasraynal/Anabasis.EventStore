using Anabasis.EventStore.Connection;
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
using Anabasis.Common.Configuration;
using Anabasis.EventStore.Standalone.Embedded;

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
            For<IEventStoreBus>().Use<EventStoreBus>();
        }
    }

    public class TestActorAutoBuildOne : BaseStatelessActor
    {
        public TestActorAutoBuildOne(IActorConfigurationFactory actorConfigurationFactory, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, loggerFactory)
        {
        }

        public TestActorAutoBuildOne(IActorConfiguration actorConfiguration, ILoggerFactory loggerFactory = null) : base(actorConfiguration, loggerFactory)
        {
        }

        public List<IEvent> Events { get; } = new List<IEvent>();


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
        public TestActorAutoBuildTwo(IActorConfigurationFactory actorConfigurationFactory, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, loggerFactory)
        {
        }

        public TestActorAutoBuildTwo(IActorConfiguration actorConfiguration, ILoggerFactory loggerFactory = null) : base(actorConfiguration, loggerFactory)
        {
        }

        public List<IEvent> Events { get; } = new List<IEvent>();


        public async Task Handle(SomeCommand someCommand)
        {
            await this.EmitEventStore(new SomeCommandResponse(someCommand.EventId, someCommand.CorrelationId, someCommand.EntityId));
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
    public class TestEventStoreActorBuilder
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
            var connectionMonitor = new EventStoreConnectionStatusMonitor(connection, _loggerFactory);

            var eventProvider = new ConsumerBasedEventProvider<TestActorAutoBuildOne>();

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();

            var actorConfiguration = new ActorConfiguration();

            var eventStoreRepository = new EventStoreRepository(
              eventStoreRepositoryConfiguration,
              connection,
              connectionMonitor,
              _loggerFactory);

            await Task.Delay(1000);

            var eventStoreBus = new EventStoreBus(connectionMonitor, eventStoreRepository);

            var testActorAutoBuildOne = new TestActorAutoBuildOne(actorConfiguration, _loggerFactory);
            await testActorAutoBuildOne.ConnectTo(eventStoreBus);
            var testActorAutoBuildTwo = new TestActorAutoBuildOne(actorConfiguration, _loggerFactory);
            await testActorAutoBuildTwo.ConnectTo(eventStoreBus);

            testActorAutoBuildOne.SubscribeToPersistentSubscriptionStream(_streamId, _groupIdOne);
            testActorAutoBuildOne.SubscribeToAllStreams(Position.End);

            await Task.Delay(2000);

            await testActorAutoBuildTwo.EmitEventStore(new SomeMoreData(_correlationId, "some-stream"));

            await Task.Delay(1000);

            Assert.AreEqual(1, testActorAutoBuildOne.Events.Count);

            await testActorAutoBuildTwo.EmitEventStore(new SomeMoreData(_correlationId, _streamId));

            await Task.Delay(100);

            Assert.AreEqual(3, testActorAutoBuildOne.Events.Count);

            eventStoreBus.Dispose();
            testActorAutoBuildOne.Dispose();
            testActorAutoBuildTwo.Dispose();
        }

        [Test, Order(1)]
        public async Task ShouldBuildFromActorBuilderAndRunActors()
        {

            var testActorAutoBuildOne = EventStoreEmbeddedStatelessActorBuilder<TestActorAutoBuildOne, SomeRegistry>.Create(_clusterVNode, _connectionSettings, loggerFactory: _loggerFactory)
                                                                                         .WithBus<IEventStoreBus>((actor, bus) =>
                                                                                         {
                                                                                             actor.SubscribeToAllStreams(Position.End);
                                                                                             actor.SubscribeToPersistentSubscriptionStream(_streamId2, _groupIdOne);
                                                                                         })
                                                                                         .Build();

            var testActorAutoBuildTwo = EventStoreEmbeddedStatelessActorBuilder<TestActorAutoBuildOne, SomeRegistry>.Create(_clusterVNode, _connectionSettings, loggerFactory: _loggerFactory)
                                                                                         .WithBus<IEventStoreBus>((actor, bus) =>
                                                                                         {
                                                                                             actor.SubscribeToAllStreams(Position.End);

                                                                                         })
                                                                                         .Build();


            await Task.Delay(1000);

            await testActorAutoBuildTwo.EmitEventStore(new SomeMoreData(_correlationId, "some-stream"));

            await Task.Delay(2000);

            Assert.AreEqual(1, testActorAutoBuildOne.Events.Count);

            await testActorAutoBuildTwo.EmitEventStore(new SomeMoreData(_correlationId, _streamId2));

            await Task.Delay(1000);

            Assert.AreEqual(3, testActorAutoBuildOne.Events.Count);

            testActorAutoBuildOne.Dispose();
            testActorAutoBuildTwo.Dispose();
        }

    }
}
