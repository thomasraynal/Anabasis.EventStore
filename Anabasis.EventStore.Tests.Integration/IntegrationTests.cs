using Anabasis.Api.Tests.Common;
using Anabasis.Common;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Standalone;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Integration.Tests
{

    [TestFixture]
    public class IntegrationTests
    {

        [OneTimeSetUp]
        public void SetUp()
        {
            if (TestHelper.IsAppVeyor)
            {
                Assert.Ignore("Issue on CI. To fix.");
            }
        }

        [Test, Order(1)]
        public async Task ShouldRunAnIntegrationScenario()
        {

            var url = new Uri("tcp://admin:changeit@localhost:1113");

            var userCredentials = new UserCredentials("admin", "changeit");

            var connectionSettings = ConnectionSettings.Create()
                            .UseDebugLogger()
                            .DisableTls()
                            .SetDefaultUserCredentials(userCredentials)
                            .KeepRetrying()
                            .Build();

            var allStreamsCatchupCacheConfiguration = new AllStreamsCatchupCacheConfiguration();

            var defaultEventTypeProvider = new DefaultEventTypeProvider<CurrencyPair>(() => new[] { typeof(CurrencyPairPriceChanged), typeof(CurrencyPairStateChanged) });

            var traderOne = EventStoreStatefulActorBuilder<Trader, AllStreamsCatchupCacheConfiguration, CurrencyPair, TestRegistry>.Create(url, connectionSettings, allStreamsCatchupCacheConfiguration, ActorConfiguration.Default, defaultEventTypeProvider)
                                                                                              .Build();
            await Task.Delay(2000);

            Assert.IsTrue(traderOne.IsConnected);

            var traderTwo = EventStoreStatefulActorBuilder<Trader, AllStreamsCatchupCacheConfiguration, CurrencyPair, TestRegistry>.Create(url, connectionSettings, allStreamsCatchupCacheConfiguration, ActorConfiguration.Default, defaultEventTypeProvider)
                                                                                            .Build();

            await Task.Delay(500);

            Assert.IsTrue(traderTwo.IsConnected);

            await Task.Delay(4000);

            var eurodolOne = traderOne.GetCurrent("EUR/USD");
            var eurodolTwo = traderTwo.GetCurrent("EUR/USD");
            var chunnelOne = traderOne.GetCurrent("EUR/GBP");
            var chunnelTwo = traderTwo.GetCurrent("EUR/GBP");

            Assert.Greater(eurodolOne.AppliedEvents.Length, 0);
            Assert.Greater(eurodolTwo.AppliedEvents.Length, 0);
            Assert.Greater(chunnelOne.AppliedEvents.Length, 0);
            Assert.Greater(chunnelTwo.AppliedEvents.Length, 0);

        }


        [OneTimeTearDown]
        public void TearDown()
        {
        }

    }
}
