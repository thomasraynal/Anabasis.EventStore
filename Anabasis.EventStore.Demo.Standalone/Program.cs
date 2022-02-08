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

                var marketDataService = EventStoreStatelessActorBuilder<MarketDataService, DemoSystemRegistry>
                                                .Create(StaticData.ClusterVNode, connectionSettings, ActorConfiguration.Default)
                                                .Build();

                var tradeService = EventStoreStatelessActorBuilder<TradeService, DemoSystemRegistry>
                                                .Create(StaticData.ClusterVNode, connectionSettings, ActorConfiguration.Default)
                                                .WithSubscribeFromEndToAllStream()
                                                .Build();

                var tradePriceUpdateService = EventStoreStatefulActorBuilder<TradePriceUpdateService, Trade, DemoSystemRegistry>
                                                .Create(StaticData.ClusterVNode, connectionSettings, ActorConfiguration.Default)
                                                .WithReadAllFromStartCache(eventTypeProvider: tradeDataEventProvider)
                                                .WithSubscribeFromEndToAllStream()
                                                .Build();

                var tradeSink = EventStoreStatefulActorBuilder<TradeSink, Trade, DemoSystemRegistry>
                                                .Create(StaticData.ClusterVNode, connectionSettings, ActorConfiguration.Default)
                                                .WithReadAllFromStartCache(eventTypeProvider: tradeDataEventProvider)
                                                .Build();

                var marketDataSink = EventStoreStatefulActorBuilder<MarketDataSink, MarketData, DemoSystemRegistry>
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
