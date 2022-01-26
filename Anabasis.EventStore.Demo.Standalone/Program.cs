using Anabasis.Common;
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
                var tradeDataEventProvider = new DefaultEventTypeProvider<Trade>(() => new[] { typeof(TradeCreated), typeof(TradeStatusChanged) });
                var marketDataEventProvider = new DefaultEventTypeProvider<MarketData>(() => new[] { typeof(MarketDataChanged) });

                var marketDataService = StatelessActorBuilder<MarketDataService, DemoSystemRegistry>
                                                .Create(StaticData.ClusterVNode, connectionSettings, ActorConfiguration.Default)
                                                .Build();

                var tradeService = StatelessActorBuilder<TradeService, DemoSystemRegistry>
                                                .Create(StaticData.ClusterVNode, connectionSettings, ActorConfiguration.Default)
                                                .WithSubscribeFromEndToAllStream()
                                                .Build();

                var tradePriceUpdateService = StatefulActorBuilder<TradePriceUpdateService, Trade, DemoSystemRegistry>
                                                .Create(StaticData.ClusterVNode, connectionSettings, ActorConfiguration.Default)
                                                .WithReadAllFromStartCache(eventTypeProvider: tradeDataEventProvider)
                                                .WithSubscribeFromEndToAllStream()
                                                .Build();

                var tradeSink = StatefulActorBuilder<TradeSink, Trade, DemoSystemRegistry>
                                                .Create(StaticData.ClusterVNode, connectionSettings, ActorConfiguration.Default)
                                                .WithReadAllFromStartCache(eventTypeProvider: tradeDataEventProvider)
                                                .Build();

                var marketDataSink = StatefulActorBuilder<MarketDataSink, MarketData, DemoSystemRegistry>
                                                .Create(StaticData.ClusterVNode, connectionSettings, ActorConfiguration.Default)
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
