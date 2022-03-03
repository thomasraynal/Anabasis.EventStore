using Anabasis.EventStore.Standalone;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using EventStore.Core;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Threading.Tasks;
using System;
using System.Reactive.Linq;
using System.Collections.Generic;
using Anabasis.Common;
using Anabasis.Common.Configuration;
using Lamar;
using Anabasis.EventStore.Standalone.Embedded;

namespace Anabasis.RabbitMQ.Tests.Integration
{
    public class TestRabbitMqActor : BaseStatelessActor
    {
        public TestRabbitMqActor(IActorConfigurationFactory actorConfigurationFactory, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, loggerFactory)
        {
        }

        public TestRabbitMqActor(IActorConfiguration actorConfiguration, ILoggerFactory loggerFactory = null) : base(actorConfiguration, loggerFactory)
        {
        }

        public List<IEvent> Events { get; } = new();

 

        public Task Handle(TestEventOne testEventOne)
        {
            Events.Add(testEventOne);

            return Task.CompletedTask;
        }


    }

    public class SomeRegistry : ServiceRegistry
    {
        public SomeRegistry()
        {
        }
    }

    [TestFixture]
    public class IntegrationActor
    {
        private RabbitMqBus _rabbitMqBus;
        private string _integrationActorExchange;
        private UserCredentials _userCredentials;
        private ConnectionSettings _connectionSettings;
        private LoggerFactory _loggerFactory;
        private ClusterVNode _clusterVNode;
        private TestRabbitMqActor _testRabbitMqActor;

        [OneTimeSetUp]
        public async Task SetUp()
        {
            _rabbitMqBus = IntegrationTestsHelper.GetRabbitMqBus();
            _integrationActorExchange = "integration-actor-exchange";
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

        [Test, Order(1)]
        public async Task ShouldCreateSusbscriptionAndConsumeAnEvent()
        {

             _testRabbitMqActor = EventStoreEmbeddedStatelessActorBuilder<TestRabbitMqActor, SomeRegistry>
                                            .Create(_clusterVNode, _connectionSettings, ActorConfiguration.Default, _loggerFactory)
                                            .Build();

            _testRabbitMqActor.ConnectTo(_rabbitMqBus, true);
        
            var onEvent = _rabbitMqBus.SubscribeRabbitMq<TestEventZero>(_integrationActorExchange);

            TestEventZero testEventZero = null;

            var disposable = onEvent.Subscribe((ev) =>
            {
                testEventZero = ev;
            });

            var eventZero = new TestEventZero(Guid.NewGuid(), Guid.NewGuid());
            var eventOne = new TestEventOne(Guid.NewGuid(), Guid.NewGuid());

            _testRabbitMqActor.EmitRabbitMq(eventOne, _integrationActorExchange);
            _testRabbitMqActor.EmitRabbitMq(eventZero, _integrationActorExchange);

            await Task.Delay(500);

            Assert.IsNotNull(testEventZero);
            Assert.AreEqual(eventZero.MessageId, testEventZero.MessageId);

            disposable.Dispose();

        }

        [Test, Order(2)]
        public async Task ShouldSubscribeAndHandleWithConsumer()
        {


            _testRabbitMqActor.SubscribeRabbitMq<TestEventOne>(_integrationActorExchange);

            var eventOne = new TestEventOne(Guid.NewGuid(), Guid.NewGuid());

            _testRabbitMqActor.EmitRabbitMq(eventOne, _integrationActorExchange);


            await Task.Delay(500);

            Assert.IsNotNull(_testRabbitMqActor.Events.Count > 0);


        }

    }
}
