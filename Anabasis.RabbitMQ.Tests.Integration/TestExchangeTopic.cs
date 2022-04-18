using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.RabbitMQ.Event;
using Lamar;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using EventStore.Core;
using NUnit.Framework;
using Anabasis.EventStore.Standalone.Embedded;

namespace Anabasis.RabbitMQ.Tests.Integration
{
    public class MarketDataChanged : BaseRabbitMqEvent
    {
        public MarketDataChanged(string currencyPair, Guid eventID, Guid correlationId) : base(eventID, eventID, correlationId)
        {
            CurrencyPair = currencyPair;
        }

        [RoutingPosition(0)]
        public string CurrencyPair { get; private set; }

    }

    public class TestMarketDataChangedActorRegistry : ServiceRegistry
    {
        public TestMarketDataChangedActorRegistry()
        {
            var rabbitMqBus = IntegrationTestsHelper.GetRabbitMqBus();

            For<IRabbitMqBus>().Use(rabbitMqBus);
        }
    }

    public class TestMarketDataChangedActor : BaseStatelessActor
    {
        public TestMarketDataChangedActor(IActorConfigurationFactory actorConfigurationFactory, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, loggerFactory)
        {
        }

        public TestMarketDataChangedActor(IActorConfiguration actorConfiguration, ILoggerFactory loggerFactory = null) : base(actorConfiguration, loggerFactory)
        {
        }

        public List<MarketDataChanged> Events { get; } = new();

        public Task Handle(MarketDataChanged marketDataChanged)
        {
            Events.Add(marketDataChanged);

            return Task.CompletedTask;
        }

    }

    public class TestExchangeTopic
    {
        private UserCredentials _userCredentials;
        private ConnectionSettings _connectionSettings;
        private LoggerFactory _loggerFactory;
        private ClusterVNode _clusterVNode;
        private TestMarketDataChangedActor _testSingleQueueMqActor;

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
        public void ShouldCreateActorAndCreateATopicExchange()
        {
            var actorQueue = typeof(TestMarketDataChangedActor).Name;

            _testSingleQueueMqActor = EventStoreEmbeddedStatelessActorBuilder<TestMarketDataChangedActor, TestSingleQueueMqActorRegistry>
                               .Create(_clusterVNode, _connectionSettings, ActorConfiguration.Default, _loggerFactory)
                               .WithBus<IRabbitMqBus>((actor, bus) =>
                               {
                                   actor.SubscribeToExchange<MarketDataChanged>(
                                            exchange: actorQueue,
                                            routingStrategy: (marketDataChanged)=> marketDataChanged.CurrencyPair == "EURUSD",
                                            queueName: actorQueue,
                                            isExchangeDurable: false,
                                            isExchangeAutoDelete: true,
                                            isQueueDurable: false,
                                            isQueueAutoAck: true,
                                            isQueueAutoDelete: true);

                                   actor.SubscribeToExchange<MarketDataChanged>(
                                            exchange: actorQueue,
                                            routingStrategy: (marketDataChanged) => marketDataChanged.CurrencyPair == "GBPUSD",
                                            queueName: actorQueue,
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
            var actorQueue = typeof(TestMarketDataChangedActor).Name;

            var marketDataChangedOne = new MarketDataChanged("EURUSD", Guid.NewGuid(), Guid.NewGuid());
            var marketDataChangedTwo = new MarketDataChanged("EURUSD", Guid.NewGuid(), Guid.NewGuid());
            var marketDataChangedThree = new MarketDataChanged("GBPUSD", Guid.NewGuid(), Guid.NewGuid());
            var marketDataChangedFour = new MarketDataChanged("EURJPY", Guid.NewGuid(), Guid.NewGuid());

            _testSingleQueueMqActor.EmitRabbitMq(marketDataChangedOne, actorQueue, isMessagePersistent: false);
            _testSingleQueueMqActor.EmitRabbitMq(marketDataChangedTwo, actorQueue, isMessagePersistent: false);
            _testSingleQueueMqActor.EmitRabbitMq(marketDataChangedThree, actorQueue, isMessagePersistent: false);
            _testSingleQueueMqActor.EmitRabbitMq(marketDataChangedFour, actorQueue, isMessagePersistent: false);

            await Task.Delay(500);

            Assert.AreEqual(3, _testSingleQueueMqActor.Events.Count);
        }

    }
}
