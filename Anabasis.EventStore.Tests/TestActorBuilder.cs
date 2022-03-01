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
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Tests
{
    public class TestActorMessage : IMessage
    {
        public TestActorMessage(IEvent @event)
        {
            Content = @event;
        }

        public Guid MessageId => Guid.NewGuid();

        public IEvent Content { get; }

        public Task Acknowledge()
        {
            return Task.CompletedTask;
        }

        public Task NotAcknowledge(string reason=null)
        {
            return Task.CompletedTask;
        }
    }

    public interface ITestActorBuilderDummyBus : IBus
    {
        void Push(IEvent push);
        void Subscribe(Action<IMessage> onEventReceived);
    }
    public class TestActorBuilderDummyBusRegistry : ServiceRegistry
    {
        public TestActorBuilderDummyBusRegistry()
        {
            For<ITestActorBuilderDummyBus>().Use<TestActorDummyBus>().Singleton();
        }
    }

    public class TestActorDummyBusConnectionMonitor : IConnectionStatusMonitor
    {
        public bool IsConnected => true;

        public ConnectionInfo ConnectionInfo => new ConnectionInfo(ConnectionStatus.Connected, 1);

        public IObservable<bool> OnConnected => throw new NotImplementedException();

        public void Dispose()
        {

        }
    }

    public class TestActorDummyBus : ITestActorBuilderDummyBus
    {
        private readonly List<Action<IMessage>> _subscribers;

        public TestActorDummyBus()
        {
            _subscribers = new List<Action<IMessage>>();
        }

        public string BusId => nameof(TestActorDummyBus);

        public bool IsConnected => true;

        public bool IsInitialized => true;

        public IConnectionStatusMonitor ConnectionStatusMonitor => new TestActorDummyBusConnectionMonitor();

        public void Dispose()
        {
        }

        public Task WaitUntilConnected(TimeSpan? timeout = null)
        {
            return Task.CompletedTask;
        }

        public void Push(IEvent push)
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber(new TestActorMessage(push));
            }
        }


        public Task Initialize()
        {
            return Task.CompletedTask;
        }

        public void Subscribe(Action<IMessage> onMessageReceived)
        {
            _subscribers.Add(onMessageReceived);
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Healthy());
        }
    }

    public static class TestActorDummyBusExtensions
    {
        public static void SubscribeTestActorDummyBus(this IActor actor, string subject)
        {
            var dummyBus = actor.GetConnectedBus<TestActorDummyBus>();

            void onEventReceived(IMessage message)
            {
                actor.OnMessageReceived(message);
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
