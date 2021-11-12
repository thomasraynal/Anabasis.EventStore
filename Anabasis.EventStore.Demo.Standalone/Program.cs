using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Standalone;
using System;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Demo
{

    class Program
    {
        static void Main(string[] args)
        {

            Task.Run(() =>
            {

                var connectionSettings = StaticData.GetConnectionSettings();
                var tradeDataEventProvider = new DefaultEventTypeProvider<long, Trade>(() => new[] { typeof(TradeCreated), typeof(TradeStatusChanged) });
                var marketDataEventProvider = new DefaultEventTypeProvider<string, MarketData>(() => new[] { typeof(MarketDataChanged) });

                var marketDataService = StatelessActorBuilder<MarketDataService, DemoSystemRegistry>
                                                .Create(StaticData.ClusterVNode, connectionSettings)
                                                .Build();

                var tradeService = StatelessActorBuilder<TradeService, DemoSystemRegistry>
                                                .Create(StaticData.ClusterVNode, connectionSettings)
                                                .WithSubscribeFromEndToAllStream()
                                                .Build();

                var tradePriceUpdateService = StatefulActorBuilder<TradePriceUpdateService, long, Trade, DemoSystemRegistry>
                                                .Create(StaticData.ClusterVNode, connectionSettings)
                                                .WithReadAllFromStartCache(eventTypeProvider: tradeDataEventProvider)
                                                .WithSubscribeFromEndToAllStream()
                                                .Build();

                var tradeSink = StatefulActorBuilder<TradeSink, long, Trade, DemoSystemRegistry>
                                                .Create(StaticData.ClusterVNode, connectionSettings)
                                                .WithReadAllFromStartCache(eventTypeProvider: tradeDataEventProvider)
                                                .Build();

                var marketDataSink = StatefulActorBuilder<MarketDataSink, string, MarketData, DemoSystemRegistry>
                                                .Create(StaticData.ClusterVNode, connectionSettings)
                                                .WithReadAllFromStartCache(eventTypeProvider: marketDataEventProvider)
                                                .Build();

                var marketDataCache = marketDataSink.State.AsObservableCache().Connect();
                var tradeCache = tradeSink.State.AsObservableCache().Connect();

                tradeCache.PrintTradeChanges();

                marketDataCache.PrintMarketDataChanges();

            });

            Console.Read();

        }
    }
}
