using Anabasis.Common;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Shared;
using Anabasis.EventStore.Standalone;
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

    public class TestStatefulActorOne : BaseStatefulActor<SomeDataAggregate>
    {
        public List<IEvent> Events { get; } = new List<IEvent>();

        public TestStatefulActorOne(IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<SomeDataAggregate> eventStoreCache, ISomeDependency _) : base(eventStoreRepository, eventStoreCache)
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

    public class TestAggregatedActorTwo : BaseStatefulActor<SomeDataAggregate>
    {

        public List<IEvent> Events { get; } = new List<IEvent>();

        public TestAggregatedActorTwo(IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<SomeDataAggregate> eventStoreCache, ISomeDependency _) : base(eventStoreRepository, eventStoreCache)
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

        [Ignore("Non deterministic")]
        [Test, Order(1)]
        public async Task ShouldBuildFromActorBuilderAndRunActors()
        {

            var defaultEventTypeProvider = new DefaultEventTypeProvider<SomeDataAggregate>(() => new[] { typeof(SomeData) });

            var testActorAutoBuildOne = StatefulActorBuilder<TestStatefulActorOne, SomeDataAggregate, SomeRegistry>.Create(_clusterVNode, _connectionSettings, _loggerFactory)
                                                                                         .WithReadAllFromStartCache(
                                                                                            getCatchupEventStoreCacheConfigurationBuilder: (conf) => conf.KeepAppliedEventsOnAggregate = true,
                                                                                            eventTypeProvider: new DefaultEventTypeProvider<SomeDataAggregate>(() => new[] { typeof(SomeData) }))
                                                                                         .WithSubscribeFromEndToAllStream()
                                                                                         .WithPersistentSubscriptionStream(_streamId2, _groupIdOne)
                                                                                         .Build();

            var testActorAutoBuildTwo = StatefulActorBuilder<TestAggregatedActorTwo, SomeDataAggregate, SomeRegistry>.Create(_clusterVNode, _connectionSettings, _loggerFactory)
                                                                                         .WithReadAllFromStartCache(
                                                                                            getCatchupEventStoreCacheConfigurationBuilder: (conf) => conf.KeepAppliedEventsOnAggregate = true,
                                                                                            eventTypeProvider: new DefaultEventTypeProvider<SomeDataAggregate>(() => new[] { typeof(SomeData) }))
                                                                                         .Build();

            await testActorAutoBuildTwo.EmitEventStore(new SomeMoreData(_correlationId, "some-stream"));

            await Task.Delay(1000);

            Assert.AreEqual(1, testActorAutoBuildOne.Events.Count);

            await testActorAutoBuildTwo.EmitEventStore(new SomeMoreData(_correlationId, _streamId2));

            await Task.Delay(1500);

            Assert.AreEqual(3, testActorAutoBuildOne.Events.Count);

            var aggregateOne = Guid.NewGuid();
            var aggregateTwo = Guid.NewGuid();

            await testActorAutoBuildOne.EmitEventStore(new SomeData($"{aggregateOne}", _correlationId));
            await testActorAutoBuildOne.EmitEventStore(new SomeData($"{aggregateTwo}", _correlationId));

            await Task.Delay(500);

            Assert.AreEqual(2, testActorAutoBuildOne.State.GetCurrents().Length);
            Assert.AreEqual(2, testActorAutoBuildTwo.State.GetCurrents().Length);

            Assert.AreEqual(1, testActorAutoBuildOne.State.GetCurrent($"{aggregateOne}").AppliedEvents.Length);
            Assert.AreEqual(1, testActorAutoBuildTwo.State.GetCurrent($"{aggregateTwo}").AppliedEvents.Length);

            await testActorAutoBuildOne.EmitEventStore(new SomeData($"{aggregateOne}", _correlationId));
            await testActorAutoBuildOne.EmitEventStore(new SomeData($"{aggregateTwo}", _correlationId));

            await Task.Delay(1000);

            Assert.AreEqual(2, testActorAutoBuildOne.State.GetCurrents().Length);
            Assert.AreEqual(2, testActorAutoBuildTwo.State.GetCurrents().Length);

            Assert.AreEqual(2, testActorAutoBuildOne.State.GetCurrent($"{aggregateOne}").AppliedEvents.Length);
            Assert.AreEqual(2, testActorAutoBuildTwo.State.GetCurrent($"{aggregateTwo}").AppliedEvents.Length);
        }

    }
}
