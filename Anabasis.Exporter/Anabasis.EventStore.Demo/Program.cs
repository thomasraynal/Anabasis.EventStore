using EventStore.ClientAPI;
using EventStore.ClientAPI.Embedded;
using EventStore.ClientAPI.SystemData;
using EventStore.Common.Options;
using System;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Demo
{
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


        var userCredentials = new UserCredentials("admin", "changeit");
        var connectionSettings = ConnectionSettings.Create().UseDebugLogger().KeepRetrying().Build();

        //MarketDataService => Repo only
        //TradeService => Repo only
        //NearToMarketService => TradeQueue
        //TradePriceUpdateJob => TradeQueue, MarketDataServiceQueue
        //TradesByPercentDiff => TradeQueue
        //TradesByTime => TradeQueue
        //TradeGenerator=> TradeQueue

      });

      Console.Read();

    }
  }
}
