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
    public class DoWorkEvent : BaseRabbitMqEvent
    {
        public DoWorkEvent(string data, Guid eventID, Guid correlationId) : base(eventID, eventID, correlationId)
        {
            Data = data;
        }

        [RoutingPosition(0)]
        public string Data { get; set; }
    }

    public class TestWorkerActorRegistry : ServiceRegistry
    {
        public TestWorkerActorRegistry()
        {
            var rabbitMqBus = IntegrationTestsHelper.GetRabbitMqBus();

            For<IRabbitMqBus>().Use(rabbitMqBus);
        }
    }

    public class TestWorkerActor : BaseStatelessActor
    {
        public TestWorkerActor(IActorConfigurationFactory actorConfigurationFactory, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, loggerFactory)
        {
        }

        public TestWorkerActor(IActorConfiguration actorConfiguration, ILoggerFactory loggerFactory = null) : base(actorConfiguration, loggerFactory)
        {
        }

        public List<IEvent> Events { get; } = new();

        public Task Handle(DoWorkEvent doWorkEvent)
        {
            Events.Add(doWorkEvent);

            return Task.CompletedTask;
        }

    }

    public class TestExchangeFanout
    {
        private UserCredentials _userCredentials;
        private ConnectionSettings _connectionSettings;
        private LoggerFactory _loggerFactory;
        private ClusterVNode _clusterVNode;
        private TestWorkerActor _testWorkerActor;

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
            var actorQueue = typeof(TestWorkerActor).Name;

            _testWorkerActor = EventStoreEmbeddedStatelessActorBuilder<TestWorkerActor, TestWorkerActorRegistry>
                               .Create(_clusterVNode, _connectionSettings, loggerFactory: _loggerFactory)
                               .WithBus<IRabbitMqBus>((actor, bus) =>
                               {
                                   actor.SubscribeToExchange<DoWorkEvent>(
                                            exchange: actorQueue,
                                            queueName: actorQueue,
                                            routingStrategy: (doWorkEvent) => doWorkEvent.Data == "none",
                                            exchangeType: "fanout",
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
            var actorQueue = typeof(TestWorkerActor).Name;

            var eventZero = new DoWorkEvent("one", Guid.NewGuid(), Guid.NewGuid());
            var eventOne = new DoWorkEvent("two", Guid.NewGuid(), Guid.NewGuid());
            var eventTwo = new DoWorkEvent("three", Guid.NewGuid(), Guid.NewGuid());

            _testWorkerActor.EmitRabbitMq(eventOne, actorQueue, "fanout", isMessagePersistent: false);
            _testWorkerActor.EmitRabbitMq(eventZero, actorQueue, "fanout", isMessagePersistent: false);
            _testWorkerActor.EmitRabbitMq(eventTwo, actorQueue, "fanout", isMessagePersistent: false);

            await Task.Delay(500);

            Assert.AreEqual(3, _testWorkerActor.Events.Count);
        }
    }
}
