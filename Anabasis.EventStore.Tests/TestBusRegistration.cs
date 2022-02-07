using Anabasis.Common;
using Anabasis.Common.Configuration;
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
using Lamar;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Tests
{
    public interface IDummyBus: IBus
    {
        void Push(IEvent push);
        void Subscribe(Action<IEvent> onEventReceived);
    }
    public class DummyBusRegistry : ServiceRegistry
    {
        public DummyBusRegistry()
        {
            For<IDummyBus>().Use<DummyBus>();
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

    public static class DummyBusExtensions
    {
        public static void SubscribeDummyBus(this IActor actor, string subject)
        {
            var dummyBus = actor.GetConnectedBus<DummyBus>();

            void onEventReceived(IEvent @event)
            {
                actor.OnEventReceived(@event);
            }

            dummyBus.Subscribe(onEventReceived);

           

        }
    }

    public class TestBusRegistrationActor : BaseEventStoreStatelessActor
    {
        public List<SomeData> Events { get; }

        public TestBusRegistrationActor(IActorConfiguration actorConfiguration, IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, loggerFactory)
        {
            Events = new List<SomeData>();
        }

        public TestBusRegistrationActor(IActorConfigurationFactory actorConfigurationFactory, IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, eventStoreRepository, loggerFactory)
        {
        }

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
        public async Task ShouldRegisterABus()
        {
            var testBusRegistrationActor = StatelessActorBuilder<TestBusRegistrationActor, DummyBusRegistry>
                                                 .Create(_clusterVNode, _connectionSettings, ActorConfiguration.Default, _loggerFactory)
                                                 .WithBus<IDummyBus>((actor,bus)=>
                                                 {
                                                     actor.SubscribeDummyBus("somesubject");
                                                 })
                                                 .Build();

            var dummyBus = testBusRegistrationActor.GetConnectedBus<IDummyBus>();

            dummyBus.Push(new SomeData("entity", Guid.NewGuid()));

            await Task.Delay(200);

            Assert.AreEqual(1, testBusRegistrationActor.Events.Count);
        }

    }
}
