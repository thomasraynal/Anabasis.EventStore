using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Standalone;
using Anabasis.EventStore.Tests;
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
using RabbitMQ.Client;

namespace Anabasis.RabbitMQ.Tests.Integration
{
    [TestFixture]
    public class IntegrationActor
    {
        private RabbitMqBus _rabbitMqBus;
        private string _integrationActorExchange;
        private UserCredentials _userCredentials;
        private ConnectionSettings _connectionSettings;
        private LoggerFactory _loggerFactory;
        private ClusterVNode _clusterVNode;

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

            var defaultEventTypeProvider = new DefaultEventTypeProvider<SomeDataAggregate>(() => new[] { typeof(SomeData) });

            var testActorAutoBuildOne = StatefulActorBuilder<TestStatefulActorOne, SomeDataAggregate, SomeRegistry>
                                                                                         .Create(_clusterVNode, _connectionSettings, _loggerFactory)
                                                                                         .WithReadAllFromStartCache(
                                                                                            getCatchupEventStoreCacheConfigurationBuilder: (conf) => conf.KeepAppliedEventsOnAggregate = true,
                                                                                            eventTypeProvider: new DefaultEventTypeProvider<SomeDataAggregate>(() => new[] { typeof(SomeData) }))
                                                                                         .WithSubscribeFromEndToAllStream()
                                                                                         .Build();



            testActorAutoBuildOne.ConnectTo(_rabbitMqBus, true);

            var onEvent = testActorAutoBuildOne.SubscribeRabbitMq<TestEventZero>(_integrationActorExchange);

            TestEventZero testEventZero = null;

            onEvent.Subscribe((ev) =>
            {
                testEventZero = ev;
            });

            var eventZero = new TestEventZero(Guid.NewGuid(), Guid.NewGuid());
            var eventOne = new TestEventOne(Guid.NewGuid(), Guid.NewGuid());

            testActorAutoBuildOne.EmitRabbitMq(eventOne, _integrationActorExchange);
            testActorAutoBuildOne.EmitRabbitMq(eventZero, _integrationActorExchange);

            await Task.Delay(500);

            Assert.IsNotNull(testEventZero);
            Assert.AreEqual(eventZero.EventID, testEventZero.EventID);
        }

    }
}
