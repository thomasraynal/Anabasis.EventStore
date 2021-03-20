using Anabasis.Actor.Actor;
using Anabasis.EventStore.Infrastructure;
using Anabasis.EventStore.Snapshot;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.Tests.Integration
{



  [TestFixture]
  public class IntegrationTests
  {
    private DockerEventStoreFixture _dockerEventStoreFixture;
    private readonly IPEndPoint _httpEndpoint = new IPEndPoint(IPAddress.Loopback, 2113);


    class DummyLogger : ILogger
    {
      public void Error(string format, params object[] args)
      { }
      public void Error(Exception ex, string format, params object[] args)
      { }
      public void Debug(string format, params object[] args) { }
      public void Debug(Exception ex, string format, params object[] args) { }
      public void Info(string format, params object[] args)
      { }
      public void Info(Exception ex, string format, params object[] args)
      { }
    }

    [OneTimeSetUp]
    public async Task SetUp()
    {
      _dockerEventStoreFixture = new DockerEventStoreFixture();

      await _dockerEventStoreFixture.Initialize();
    }

    [Test]
    public async Task ShouldRunAnIntegrationScenario()
    {

      var url = "tcp://admin:changeit@localhost:1113";

      var userCredentials = new UserCredentials("admin", "changeit");
      var connectionSettings = ConnectionSettings.Create().UseDebugLogger().KeepRetrying().DisableTls().Build();



      //var projectionsManager = new ProjectionsManager(
      //    log: new ConsoleLogger(),
      //    httpEndPoint: new IPEndPoint(IPAddress.Loopback, 2113),
      //    operationTimeout: TimeSpan.FromMilliseconds(5000),
      //    httpSchema: "http"
      //);

      //await Task.Delay(5000);

      //var testProjection = File.ReadAllText("./Projections/testProjection.js");

      //var all = await projectionsManager.ListAllAsync();

      //await projectionsManager.CreateTransientAsync("countOf", testProjection, userCredentials);



      var defaultEventTypeProvider = new DefaultEventTypeProvider<string, CurrencyPair>(() => new[] { typeof(CurrencyPairPriceChanged), typeof(CurrencyPairStateChanged)});

      var traderOne = AggregateActorBuilder<Trader, string, CurrencyPair, TestRegistry>.Create(url, userCredentials, connectionSettings, eventTypeProvider: defaultEventTypeProvider)
                                                                                        .WithReadAllFromStartCache(eventTypeProvider: defaultEventTypeProvider,
                                                                                          catchupEventStoreCacheConfigurationBuilder: (configuration)=> configuration.KeepAppliedEventsOnAggregate = true)
                                                                                        .Build();
      await Task.Delay(1000);

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
