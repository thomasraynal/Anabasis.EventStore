using Anabasis.Common;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Demo.Bus;
using Anabasis.EventStore.Standalone;
using Anabasis.EventStore.Standalone.Embedded;
using EventStore.ClientAPI;
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

                var connectionSettings = StaticData.GetConnectionSettings();

                var tradeDataEventProvider = new DefaultEventTypeProvider<Trade>(() => new[] { typeof(TradeCreated), typeof(TradeStatusChanged) });
                var marketDataEventProvider = new DefaultEventTypeProvider<MarketData>(() => new[] { typeof(MarketDataChanged) });

                var allStreamsCatchupCacheConfiguration = new AllStreamsCatchupCacheConfiguration();

                var tradeService = EventStoreEmbeddedStatelessActorBuilder<TradeService, DemoSystemRegistry>
                                                .Create(StaticData.ClusterVNode, connectionSettings, ActorConfiguration.Default)
                                                .WithBus<IEventStoreBus>((actor, bus) =>
                                                {
                                                    actor.SubscribeToAllStreams(Position.Start);
                                                })
                                                .Build();

                var tradePriceUpdateService = EventStoreEmbeddedStatefulActorBuilder<TradePriceUpdateService, AllStreamsCatchupCacheConfiguration, Trade, DemoSystemRegistry>
                                                .Create(StaticData.ClusterVNode, connectionSettings, ActorConfiguration.Default, allStreamsCatchupCacheConfiguration, tradeDataEventProvider)
                                                .WithBus<IEventStoreBus>((actor, bus) =>
                                                {
                                                    actor.SubscribeToAllStreams(Position.Start);
                                                })
                                                .WithBus<IMarketDataBus>((actor, bus) =>
                                                {
                                                    actor.SubscribeMarketDataBus();
                                                })
                                                .Build();

                var tradeSink = EventStoreEmbeddedStatefulActorBuilder<TradeSink, AllStreamsCatchupCacheConfiguration, Trade, DemoSystemRegistry>
                                                .Create(StaticData.ClusterVNode, connectionSettings, ActorConfiguration.Default, allStreamsCatchupCacheConfiguration, tradeDataEventProvider)
                                                .Build();

                var marketDataSink = StatelessActorBuilder<MarketDataSink, DemoSystemRegistry>
                                                .Create(ActorConfiguration.Default)
                                                .WithBus<IMarketDataBus>((actor, bus) =>
                                                {
                                                    actor.SubscribeMarketDataBus();
                                                })
                                                .Build();

                await tradePriceUpdateService.ConnectToEventStream();
                await tradeSink.ConnectToEventStream();
        

                var marketDataCache = marketDataSink.CurrentPrices.Connect();
                var tradeCache = tradeSink.AsObservableCache().Connect();

                tradeCache.PrintTradeChanges();
                marketDataCache.PrintMarketDataChanges();

            });

            Console.Read();

        }
    }
}
