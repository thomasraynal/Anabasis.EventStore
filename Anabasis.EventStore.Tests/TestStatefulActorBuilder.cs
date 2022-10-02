using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Snapshot;
using Anabasis.EventStore.Standalone.Embedded;
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

    public class TestStatefulActorOne : SubscribeToAllStreamsEventStoreStatefulActor<SomeDataAggregate>
    {
        public TestStatefulActorOne(IActorConfigurationFactory actorConfigurationFactory, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, ILoggerFactory loggerFactory = null, ISnapshotStore<SomeDataAggregate> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, IKillSwitch killSwitch = null) : base(actorConfigurationFactory, connectionMonitor, loggerFactory, snapshotStore, snapshotStrategy, killSwitch)
        {
        }

        public TestStatefulActorOne(IActorConfiguration actorConfiguration, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, AllStreamsCatchupCacheConfiguration catchupCacheConfiguration, IEventTypeProvider eventTypeProvider, ILoggerFactory loggerFactory = null, ISnapshotStore<SomeDataAggregate> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, IKillSwitch killSwitch = null) : base(actorConfiguration, connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy, killSwitch)
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

    public class TestAggregatedActorTwo : SubscribeToAllStreamsEventStoreStatefulActor<SomeDataAggregate>
    {
        public TestAggregatedActorTwo(IActorConfigurationFactory actorConfigurationFactory, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, ILoggerFactory loggerFactory = null, ISnapshotStore<SomeDataAggregate> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, IKillSwitch killSwitch = null) : base(actorConfigurationFactory, connectionMonitor, loggerFactory, snapshotStore, snapshotStrategy, killSwitch)
        {
        }

        public TestAggregatedActorTwo(IActorConfiguration actorConfiguration, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, AllStreamsCatchupCacheConfiguration catchupCacheConfiguration, IEventTypeProvider eventTypeProvider, ILoggerFactory loggerFactory = null, ISnapshotStore<SomeDataAggregate> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, IKillSwitch killSwitch = null) : base(actorConfiguration, connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy, killSwitch)
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
    public class TestStatefulActorBuilder
    {
        private UserCredentials _userCredentials;
        private ConnectionSettings _connectionSettings;
        private LoggerFactory _loggerFactory;
        private ClusterVNode _clusterVNode;

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


            _loggerFactory = new LoggerFactory();

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
        public async Task ShouldBuildFromActorBuilderAndRunActors()
        {

            var defaultEventTypeProvider = new DefaultEventTypeProvider<SomeDataAggregate>(() => new[] { typeof(SomeDataAggregateEvent) });

            var allStreamsCatchupCacheConfiguration = new AllStreamsCatchupCacheConfiguration(Position.Start)
            {
                KeepAppliedEventsOnAggregate = true
            };

            var testActorAutoBuildOne = EventStoreEmbeddedStatefulActorBuilder<TestStatefulActorOne, AllStreamsCatchupCacheConfiguration, SomeDataAggregate, SomeRegistry >.Create(_clusterVNode, _connectionSettings, ActorConfiguration.Default, allStreamsCatchupCacheConfiguration, defaultEventTypeProvider, _loggerFactory)
                                                                                           .WithBus<IEventStoreBus>((actor, bus) =>
                                                                                             {
                                                                                                 actor.SubscribeToAllStreams(Position.Start);
                                                                                                 actor.SubscribeToPersistentSubscriptionStream(_streamId2, _groupIdOne);
                                                                                             })
                                                                                         .Build();

            var testActorAutoBuildTwo = EventStoreEmbeddedStatefulActorBuilder<TestAggregatedActorTwo, AllStreamsCatchupCacheConfiguration, SomeDataAggregate, SomeRegistry>.Create(_clusterVNode, _connectionSettings, ActorConfiguration.Default, allStreamsCatchupCacheConfiguration, defaultEventTypeProvider, _loggerFactory)
                                                                                         .WithBus<IEventStoreBus>((actor, bus) =>
                                                                                            {
                                                                                                actor.SubscribeToAllStreams(Position.Start);
                                                                                            })
                                                                                         .Build();

            await testActorAutoBuildOne.ConnectToEventStream();

            await testActorAutoBuildTwo.ConnectToEventStream();

            await testActorAutoBuildTwo.EmitEventStore(new SomeMoreData(_correlationId, "some-stream"));

            await Task.Delay(1000);

            Assert.AreEqual(1, testActorAutoBuildOne.Events.Count);

            await testActorAutoBuildTwo.EmitEventStore(new SomeMoreData(_correlationId, _streamId2));

            await Task.Delay(1500);

            Assert.AreEqual(3, testActorAutoBuildOne.Events.Count);

            var aggregateOne = Guid.NewGuid();
            var aggregateTwo = Guid.NewGuid();

            await testActorAutoBuildOne.EmitEventStore(new SomeDataAggregateEvent($"{aggregateOne}", _correlationId));
            await testActorAutoBuildOne.EmitEventStore(new SomeDataAggregateEvent($"{aggregateTwo}", _correlationId));

            await Task.Delay(500);

            Assert.AreEqual(2, testActorAutoBuildOne.GetCurrents().Length);
            Assert.AreEqual(2, testActorAutoBuildTwo.GetCurrents().Length);

            Assert.AreEqual(1, testActorAutoBuildOne.GetCurrent($"{aggregateOne}").AppliedEvents.Length);
            Assert.AreEqual(1, testActorAutoBuildTwo.GetCurrent($"{aggregateTwo}").AppliedEvents.Length);

            await testActorAutoBuildOne.EmitEventStore(new SomeDataAggregateEvent($"{aggregateOne}", _correlationId));
            await testActorAutoBuildOne.EmitEventStore(new SomeDataAggregateEvent($"{aggregateTwo}", _correlationId));

            await Task.Delay(1000);

            Assert.AreEqual(2, testActorAutoBuildOne.GetCurrents().Length);
            Assert.AreEqual(2, testActorAutoBuildTwo.GetCurrents().Length);

            Assert.AreEqual(2, testActorAutoBuildOne.GetCurrent($"{aggregateOne}").AppliedEvents.Length);
            Assert.AreEqual(2, testActorAutoBuildTwo.GetCurrent($"{aggregateTwo}").AppliedEvents.Length);

            testActorAutoBuildOne.Dispose();
            testActorAutoBuildTwo.Dispose();
        }

    }
}
