using Anabasis.EventStore.Actor;
using Anabasis.EventStore.EventProvider;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using NUnit.Framework;
using System.Net;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Tests.Integration
{



    [TestFixture, Category("Integration")]
    public class IntegrationTests
    {
        private DockerEventStoreFixture _dockerEventStoreFixture;
        private readonly IPEndPoint _httpEndpoint = new IPEndPoint(IPAddress.Loopback, 2113);

        [OneTimeSetUp]
        public async Task SetUp()
        {
            _dockerEventStoreFixture = new DockerEventStoreFixture();

            await _dockerEventStoreFixture.Initialize();
        }

        [Test, Order(1)]
        public async Task ShouldRunAnIntegrationScenario()
        {

            var url = "tcp://admin:changeit@localhost:1113";

            var userCredentials = new UserCredentials("admin", "changeit");
            var connectionSettings = ConnectionSettings.Create().UseDebugLogger().KeepRetrying().DisableTls().Build();


            var defaultEventTypeProvider = new DefaultEventTypeProvider<string, CurrencyPair>(() => new[] { typeof(CurrencyPairPriceChanged), typeof(CurrencyPairStateChanged) });

            var traderOne = AggregateActorBuilder<Trader, string, CurrencyPair, TestRegistry>.Create(url, userCredentials, connectionSettings, eventTypeProvider: defaultEventTypeProvider)
                                                                                              .WithReadAllFromStartCache(eventTypeProvider: defaultEventTypeProvider,
                                                                                                catchupEventStoreCacheConfigurationBuilder: (configuration) => configuration.KeepAppliedEventsOnAggregate = true)
                                                                                              .Build();
            await Task.Delay(2000);

            Assert.IsTrue(traderOne.State.IsConnected);

            var traderTwo = AggregateActorBuilder<Trader, string, CurrencyPair, TestRegistry>.Create(url, userCredentials, connectionSettings, eventTypeProvider: defaultEventTypeProvider)
                                                                                            .WithReadAllFromStartCache(eventTypeProvider: defaultEventTypeProvider,
                                                                                               catchupEventStoreCacheConfigurationBuilder: (configuration) => configuration.KeepAppliedEventsOnAggregate = true)
                                                                                            .Build();

            await Task.Delay(500);

            Assert.IsTrue(traderTwo.State.IsConnected);

            await Task.Delay(4000);

            var eurodolOne = traderOne.State.GetCurrent("EUR/USD");
            var eurodolTwo = traderTwo.State.GetCurrent("EUR/USD");
            var chunnelOne = traderOne.State.GetCurrent("EUR/GBP");
            var chunnelTwo = traderTwo.State.GetCurrent("EUR/GBP");

            Assert.Greater(eurodolOne.AppliedEvents.Length, 0);
            Assert.Greater(eurodolTwo.AppliedEvents.Length, 0);
            Assert.Greater(chunnelOne.AppliedEvents.Length, 0);
            Assert.Greater(chunnelTwo.AppliedEvents.Length, 0);

        }


        [OneTimeTearDown]
        public async Task TearDown()
        {
            await _dockerEventStoreFixture.Dispose();
        }

    }
}
