using Anabasis.Common;
using Anabasis.Common.Actor;
using Anabasis.Common.Configuration;
using Anabasis.EventStore.Standalone;
using Lamar;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Tests
{
    public interface ITestActorBuilderDummyBus : IBus
    {
        void Push(IEvent push);
        void Subscribe(Action<IEvent> onEventReceived);
    }
    public class TestActorBuilderDummyBusRegistry : ServiceRegistry
    {
        public TestActorBuilderDummyBusRegistry()
        {
            For<ITestActorBuilderDummyBus>().Use<TestActorDummyBus>().Singleton();
        }
    }

    public class TestActorDummyBus : ITestActorBuilderDummyBus
    {
        private readonly List<Action<IEvent>> _subscribers;

        public TestActorDummyBus()
        {
            _subscribers = new List<Action<IEvent>>();
        }

        public string BusId => nameof(TestActorDummyBus);

        public bool IsConnected => true;

        public bool IsInitialized => true;

        public IConnectionStatusMonitor ConnectionStatusMonitor => throw new NotImplementedException();

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

    public static class TestActorDummyBussExtensions
    {
        public static void SubscribeTestActorDummyBus(this IActor actor, string subject)
        {
            var dummyBus = actor.GetConnectedBus<TestActorDummyBus>();

            void onEventReceived(IEvent @event)
            {
                actor.OnEventReceived(@event);
            }

            dummyBus.Subscribe(onEventReceived);



        }
    }

    public class TestBuilderActor : BaseStatelessActor
    {
        public List<SomeData> Events { get; } = new List<SomeData>();

        public TestBuilderActor(IActorConfigurationFactory actorConfigurationFactory, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, loggerFactory)
        {
        }

        public TestBuilderActor(IActorConfiguration actorConfiguration, ILoggerFactory loggerFactory = null) : base(actorConfiguration, loggerFactory)
        {
        }

        public Task Handle(SomeData someData)
        {
            Events.Add(someData);

            return Task.CompletedTask;
        }
    }

    [TestFixture]
    public class TestActorBuilder
    {
        private LoggerFactory _loggerFactory;

        [OneTimeSetUp]
        public void Setup()
        {
            _loggerFactory = new LoggerFactory();
        }

        [Test, Order(0)]
        public async Task ShouldBuildAStatelessActorAndRegisterABus()
        {
            var testBusRegistrationActor = StatelessActorBuilder<TestBuilderActor, TestActorBuilderDummyBusRegistry>
                                                 .Create(ActorConfiguration.Default, _loggerFactory)
                                                 .WithBus<ITestActorBuilderDummyBus>((actor, bus) =>
                                                 {
                                                     actor.SubscribeTestActorDummyBus("somesubject");
                                                 })
                                                 .Build();

            var dummyBus = testBusRegistrationActor.GetConnectedBus<ITestActorBuilderDummyBus>();

            dummyBus.Push(new SomeData("entity", Guid.NewGuid()));

            await Task.Delay(200);

            Assert.AreEqual(1, testBusRegistrationActor.Events.Count);

            testBusRegistrationActor.Dispose();
        }
    }
}
