using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Mvc.Factories;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Standalone;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using EventStore.Core;
using Lamar;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Tests
{

    public interface IDummyBus2 : IBus
    {
        void Push(IEvent push);
        void Subscribe(Action<IEvent> onEventReceived);
    }
    public interface IDummyBus: IBus
    {
        void Push(IEvent push);
        void Subscribe(Action<IEvent> onEventReceived);
    }
    public class DummyBusRegistry : ServiceRegistry
    {
        public DummyBusRegistry()
        {
            For<IDummyBus>().Use<DummyBus>().Singleton();
            For<IDummyBus2>().Use<DummyBus2>().Singleton();
        }
    }

    public class DummyBus :  IDummyBus
    {
        private readonly List<Action<IEvent>> _subscribers;

        public DummyBus()
        {
            _subscribers = new List<Action<IEvent>>();
        }

        public string BusId => nameof(DummyBus);

        public bool IsConnected => true;

        public bool IsInitialized => true;

        public void Dispose()
        {
        }

        public void Push(IEvent push)
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber(push);
            }
        }

        public Task<HealthCheckResult> GetHealthCheck(bool shouldThrowIfUnhealthy = false)
        {
            return Task.FromResult(HealthCheckResult.Healthy());
        }

        public Task Initialize()
        {
            return Task.CompletedTask;
        }

        public void Subscribe(Action<IEvent> onEventReceived)
        {
            _subscribers.Add(onEventReceived);
        }
    }
    public class DummyBus2 : IDummyBus2
    {
        private readonly List<Action<IEvent>> _subscribers;

        public DummyBus2()
        {
            _subscribers = new List<Action<IEvent>>();
        }

        public string BusId => nameof(DummyBus);

        public bool IsConnected => true;

        public bool IsInitialized => true;

        public void Dispose()
        {
        }

        public void Push(IEvent push)
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber(push);
            }
        }

        public Task<HealthCheckResult> GetHealthCheck(bool shouldThrowIfUnhealthy = false)
        {
            return Task.FromResult(HealthCheckResult.Healthy());
        }

        public Task Initialize()
        {
            return Task.CompletedTask;
        }

        public void Subscribe(Action<IEvent> onEventReceived)
        {
            _subscribers.Add(onEventReceived);
        }
    }
    public static class DummyBusExtensions
    {
        public static void SubscribeDummyBus(this IActor actor, string subject)
        {
            var dummyBus = actor.GetConnectedBus<IDummyBus>();

            void onEventReceived(IEvent @event)
            {
                actor.OnEventReceived(@event);
            }

            dummyBus.Subscribe(onEventReceived);
        }

        public static void SubscribeDummyBus2(this IActor actor, string subject)
        {
            var dummyBus = actor.GetConnectedBus<IDummyBus2>();

            void onEventReceived(IEvent @event)
            {
                actor.OnEventReceived(@event);
            }

            dummyBus.Subscribe(onEventReceived);
        }
    }

    public class TestBusRegistrationStatelessActor : BaseEventStoreStatelessActor
    {
        public List<SomeData> Events { get; }

        public TestBusRegistrationStatelessActor(IActorConfiguration actorConfiguration, IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, loggerFactory)
        {
            Events = new List<SomeData>();
        }

        public TestBusRegistrationStatelessActor(IActorConfigurationFactory actorConfigurationFactory, IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, eventStoreRepository, loggerFactory)
        {
            Events = new List<SomeData>();
        }

        public Task Handle(SomeData someData)
        {
            Events.Add(someData);

            return Task.CompletedTask;
        }
    }

    public class TestBusRegistrationStatefulActor : BaseEventStoreStatefulActor<SomeDataAggregate>
    {
        public TestBusRegistrationStatefulActor(IActorConfiguration actorConfiguration, IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<SomeDataAggregate> eventStoreCache, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, eventStoreCache, loggerFactory)
        {
        }

        public TestBusRegistrationStatefulActor(IEventStoreActorConfigurationFactory eventStoreCacheFactory, IEventStoreAggregateRepository eventStoreRepository, IConnectionStatusMonitor connectionStatusMonitor, ILoggerFactory loggerFactory = null) : base(eventStoreCacheFactory, eventStoreRepository, connectionStatusMonitor, loggerFactory)
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
    public class TestBusRegistration
    {

        private UserCredentials _userCredentials;
        private ConnectionSettings _connectionSettings;
        private LoggerFactory _loggerFactory;
        private ClusterVNode _clusterVNode;

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


        [Test, Order(0)]
        public async Task ShouldRegisterABusOnAStatelessActor()
        {
            var testBusRegistrationActor = EventStoreStatelessActorBuilder<TestBusRegistrationStatelessActor, DummyBusRegistry>
                                                 .Create(_clusterVNode, _connectionSettings, ActorConfiguration.Default, _loggerFactory)
                                                 .WithBus<IDummyBus>((actor, bus) =>
                                                 {
                                                     actor.SubscribeDummyBus("somesubject");
                                                 })
                                                 .Build();

            var dummyBus = testBusRegistrationActor.GetConnectedBus<IDummyBus>();

            dummyBus.Push(new SomeData("entity", Guid.NewGuid()));

            await Task.Delay(200);

            Assert.AreEqual(1, testBusRegistrationActor.Events.Count);

            testBusRegistrationActor.Dispose();
        }

        [Test, Order(1)]
        public async Task ShouldRegisterABusOnAStatefulActor()
        {
            var testBusRegistrationActor = EventStoreStatefulActorBuilder<TestBusRegistrationStatefulActor, SomeDataAggregate, DummyBusRegistry>
                                                 .Create(_clusterVNode, _connectionSettings, ActorConfiguration.Default, _loggerFactory)
                                                 .WithReadAllFromEndCache(new ConsumerBasedEventProvider<SomeDataAggregate, TestBusRegistrationStatefulActor>())
                                                 .WithBus<IDummyBus>((actor, bus) =>
                                                 {
                                                     actor.SubscribeDummyBus("somesubject");
                                                 })
                                                 .Build();

            var dummyBus = testBusRegistrationActor.GetConnectedBus<IDummyBus>();

            dummyBus.Push(new SomeData("entity", Guid.NewGuid()));

            await Task.Delay(200);

            Assert.AreEqual(1, testBusRegistrationActor.Events.Count);

            testBusRegistrationActor.Dispose();
        }

        [Test, Order(2)]
        public async Task ShouldRegisterTwoBusAndHandleEventsFromDifferentSources()
        {
            var testBusRegistrationActor = EventStoreStatelessActorBuilder<TestBusRegistrationStatelessActor, DummyBusRegistry>
                                                 .Create(_clusterVNode, _connectionSettings, ActorConfiguration.Default, _loggerFactory)
                                                 .WithBus<IDummyBus>((actor, bus) =>
                                                 {
                                                     actor.SubscribeDummyBus("somesubject");
                                                 })
                                                 .WithBus<IDummyBus2>((actor, bus) =>
                                                 {
                                                     actor.SubscribeDummyBus2("someothersubject");
                                                 })
                                                 .Build();

            var dummyBus = testBusRegistrationActor.GetConnectedBus<IDummyBus>();
            var dummyBus2 = testBusRegistrationActor.GetConnectedBus<IDummyBus2>();
            dummyBus.Push(new SomeData("entity", Guid.NewGuid()));
            dummyBus2.Push(new SomeData("entity2", Guid.NewGuid()));

            await Task.Delay(200);

            Assert.AreEqual(2, testBusRegistrationActor.Events.Count);

            testBusRegistrationActor.Dispose();
        }

        [Test, Order(3)]
        public async Task ShouldRegisterOneBusAndHandleEventsFromDifferentSources()
        {

            var testBusRegistrationActor = EventStoreStatelessActorBuilder<TestBusRegistrationStatelessActor, DummyBusRegistry>
                                       .Create(_clusterVNode, _connectionSettings, ActorConfiguration.Default, _loggerFactory)
                                       .WithSubscribeFromEndToAllStream()
                                       .WithBus<IDummyBus>((actor, bus) =>
                                       {
                                           actor.SubscribeDummyBus("somesubject");
                                       })
                                       .Build();


            await Task.Delay(500);

            var eventStoreRepositoryConfiguration = new EventStoreRepositoryConfiguration();
            var connection = EmbeddedEventStoreConnection.Create(_clusterVNode, _connectionSettings);
            var connectionMonitor = new ConnectionStatusMonitor(connection, _loggerFactory);

            var eventStoreRepository = new EventStoreAggregateRepository(
              eventStoreRepositoryConfiguration,
              connection,
              connectionMonitor,
              _loggerFactory);


            var dummyBus = testBusRegistrationActor.GetConnectedBus<IDummyBus>();
            dummyBus.Push(new SomeData("entity", Guid.NewGuid()));

            await eventStoreRepository.Emit(new SomeData("thisisit", Guid.NewGuid()));

            await Task.Delay(500);

            Assert.AreEqual(2, testBusRegistrationActor.Events.Count);
            Assert.True(testBusRegistrationActor.Events.Any(ev => ev.EntityId == "thisisit"));

            testBusRegistrationActor.Dispose();
        }

    }
}
