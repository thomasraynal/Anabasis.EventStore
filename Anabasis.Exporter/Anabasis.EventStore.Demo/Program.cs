using Anabasis.Actor.Actor;
using Anabasis.EventStore.Infrastructure;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using Lamar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Demo
{
  public class DemoSystemRegistry : ServiceRegistry
  {
    public DemoSystemRegistry()
    {
      For<IStaticData>().Use<StaticData>();
    }
  }

  public class CcyPairReporting
  {
    public static CcyPairReporting Empty = new CcyPairReporting();

    public Dictionary<string, (decimal bid, decimal offer, decimal spread, bool IsUp)> Data =
      new Dictionary<string, (decimal bid, decimal offer, decimal spread, bool IsUp)>();

    public CcyPairReporting(CcyPairReporting previous)
    {
      foreach(var keyValue in previous.Data)
      {
        Data[keyValue.Key] = keyValue.Value;
      }

    }

    public CcyPairReporting()
    {
    }
  }


  class Program
  {
   
    static void Main(string[] args)
    {


      Task.Run(async () =>
      {

        var clusterVNode = EmbeddedVNodeBuilder
              .AsSingleNode()
              .RunInMemory()
              .RunProjections(ProjectionType.All)
              .StartStandardProjections()
              .WithWorkerThreads(1)
              .Build();

        await clusterVNode.StartAsync(true);

        Console.WriteLine("Starting...");

        var userCredentials = new UserCredentials("admin", "changeit");
        var connectionSettings = ConnectionSettings.Create().UseDebugLogger().KeepRetrying().Build();

        var marketDataService = ActorBuilder<MarketDataService, DemoSystemRegistry>.Create(clusterVNode, userCredentials, connectionSettings).Build();
        //   var tradeService = ActorBuilder<TradeService, DemoSystemRegistry>.Create(clusterVNode, userCredentials, connectionSettings).Build();

        //   var tradeSink = AggregateActorBuilder<TradeSink, long, Trade, DemoSystemRegistry>.Create(clusterVNode, userCredentials, connectionSettings).Build();
        var marketDataSink = AggregateActorBuilder<MarketDataSink, string, MarketData, DemoSystemRegistry>
                                            .Create(clusterVNode, userCredentials, connectionSettings)
                                            .WithReadAllFromStartCache(
                                               eventTypeProvider: new DefaultEventTypeProvider<string, MarketData>(() => new[] { typeof(MarketDataChanged) }))
                                            .Build();


        marketDataSink.State.AsObservableCache().Connect()
        .Scan(CcyPairReporting.Empty,(previous, changeSet) =>
        {

          foreach (var change in changeSet)
          {
            var isUp = previous.Data.ContainsKey(change.Key) && change.Current.Offer > previous.Data[change.Key].offer;

            previous.Data[change.Key] = (change.Current.Bid, change.Current.Offer, (change.Current.Offer - change.Current.Bid), isUp);
          }

          return previous;

        })
        .Subscribe(ccyPairReporting =>
        {
          foreach(var ccy in ccyPairReporting.Data)
          {
            var upDown = ccy.Value.IsUp ? "UP" : "DOWN";

            Console.WriteLine($"[{ccy.Key}] => {ccy.Value.offer}/{ccy.Value.offer} {upDown}");

          }

        });




      });

      Console.Read();

    }
  }
}
