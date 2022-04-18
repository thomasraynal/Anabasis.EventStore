using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using EventStore.Core;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Anabasis.Common;
using Anabasis.Common.Configuration;
using Lamar;
using Anabasis.EventStore.Standalone.Embedded;
using Anabasis.RabbitMQ.Event;

namespace Anabasis.RabbitMQ.Tests.Integration
{
    public class SingleQueueTestEventOne : BaseRabbitMqEvent
    {
        public SingleQueueTestEventOne(Guid eventID, Guid correlationId) : base(eventID, eventID, correlationId)
        {
        }

        public override string Subject => $"{nameof(SingleQueueTestEventOne)}.#";
    }

    public class SingleQueueTestEventTwo : BaseRabbitMqEvent
    {
        public SingleQueueTestEventTwo(Guid eventID, Guid correlationId) : base(eventID, eventID, correlationId)
        {
        }

        public override string Subject => $"{nameof(SingleQueueTestEventTwo)}.#";
    }

    public class SingleQueueTestEventThree : BaseRabbitMqEvent
    {
        public SingleQueueTestEventThree(Guid eventID, Guid correlationId) : base(eventID, eventID, correlationId)
        {
        }

        public override string Subject => $"{nameof(SingleQueueTestEventThree)}.#";
    }

    public class TestSingleQueueMqActorRegistry : ServiceRegistry
    {
        public TestSingleQueueMqActorRegistry()
        {
            var rabbitMqBus = IntegrationTestsHelper.GetRabbitMqBus();

            For<IRabbitMqBus>().Use(rabbitMqBus);
        }
    }

    public class TestSingleQueueMqActor : BaseStatelessActor
    {
        public TestSingleQueueMqActor(IActorConfigurationFactory actorConfigurationFactory, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, loggerFactory)
        {
        }

        public TestSingleQueueMqActor(IActorConfiguration actorConfiguration, ILoggerFactory loggerFactory = null) : base(actorConfiguration, loggerFactory)
        {
        }

        public List<IEvent> Events { get; } = new();

        public Task Handle(SingleQueueTestEventOne singleQueueTestEventOne)
        {
            Events.Add(singleQueueTestEventOne);

            return Task.CompletedTask;
        }

        public Task Handle(SingleQueueTestEventTwo singleQueueTestEventTwo)
        {
            Events.Add(singleQueueTestEventTwo);

            return Task.CompletedTask;
        }
    }

    public class TestExchangeDirect
    {
        private UserCredentials _userCredentials;
        private ConnectionSettings _connectionSettings;
        private LoggerFactory _loggerFactory;
        private ClusterVNode _clusterVNode;
        private TestSingleQueueMqActor _testSingleQueueMqActor;

        [OneTimeSetUp]
        public async Task SetUp()
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

        [Test, Order(1)]
        public void ShouldCreateActorAndCreateADirectExchange()
        {
            var actorQueue = typeof(TestSingleQueueMqActor).Name;

            _testSingleQueueMqActor = EventStoreEmbeddedStatelessActorBuilder<TestSingleQueueMqActor, TestSingleQueueMqActorRegistry>
                               .Create(_clusterVNode, _connectionSettings, ActorConfiguration.Default, _loggerFactory)
                               .WithBus<IRabbitMqBus>((actor, bus) =>
                               {
                                   actor.SubscribeToExchange<SingleQueueTestEventOne>(
                                            exchange: actorQueue,
                                            queueName: actorQueue, 
                                            exchangeType: "direct",
                                            isExchangeDurable: false,
                                            isExchangeAutoDelete: true,
                                            isQueueDurable: false,
                                            isQueueAutoAck: true,
                                            isQueueAutoDelete: true);

                                   actor.SubscribeToExchange<SingleQueueTestEventTwo>(
                                            exchange: actorQueue,
                                            queueName: actorQueue,
                                            exchangeType: "direct",
                                            isExchangeDurable: false,
                                            isExchangeAutoDelete: true,
                                            isQueueDurable: false,
                                            isQueueAutoAck: true,
                                            isQueueAutoDelete: true);

                               })
                               .Build();

        }

        [Test, Order(2)]
        public async Task ShouldEmitAndProcessSomeEvents()
        {
            var actorQueue = typeof(TestSingleQueueMqActor).Name;

            var eventZero = new SingleQueueTestEventOne(Guid.NewGuid(), Guid.NewGuid());
            var eventOne = new SingleQueueTestEventTwo(Guid.NewGuid(), Guid.NewGuid());
            var eventTwo = new SingleQueueTestEventThree(Guid.NewGuid(), Guid.NewGuid());

            _testSingleQueueMqActor.EmitRabbitMq(eventOne, actorQueue, "direct", isMessagePersistent: false);
            _testSingleQueueMqActor.EmitRabbitMq(eventZero, actorQueue, "direct", isMessagePersistent: false);
            _testSingleQueueMqActor.EmitRabbitMq(eventTwo, actorQueue, "direct", isMessagePersistent: false);

            await Task.Delay(500);

            Assert.AreEqual(2, _testSingleQueueMqActor.Events.Count);
        }

    }
}
