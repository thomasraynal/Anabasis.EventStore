using Anabasis.Common;
using Anabasis.Common.Queue;
using Anabasis.EventStore.Demo.Bus;
using Anabasis.EventStore.Standalone;
using Anabasis.EventStore.Standalone.Embedded;
using Anabasis.RabbitMQ;
using Anabasis.RabbitMQ.Connection;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
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

                var tradeService = EventStoreEmbeddedStatelessActorBuilder<TradeService, DemoSystemRegistry>
                                                .Create(StaticData.ClusterVNode, connectionSettings, ActorConfiguration.Default)
                                                .WithBus<IEventStoreBus>((actor, bus) =>
                                                {
                                                    actor.SubscribeToAllStreams(Position.End);
                                                })
                                                .Build();

                var tradePriceUpdateService = EventStoreEmbeddedStatefulActorBuilder<TradePriceUpdateService, Trade, DemoSystemRegistry>
                                                .Create(StaticData.ClusterVNode, connectionSettings, ActorConfiguration.Default)
                                                .WithReadAllFromStartCache(eventTypeProvider: tradeDataEventProvider)
                                                .WithBus<IEventStoreBus>((actor, bus) =>
                                                {
                                                    actor.SubscribeFromEndToAllStreams();
                                                })
                                                .WithBus<IMarketDataBus>((actor, bus) =>
                                                {
                                                    actor.SubscribeMarketDataBus();
                                                })
                                                .Build();

                var tradeSink = EventStoreEmbeddedStatefulActorBuilder<TradeSink, Trade, DemoSystemRegistry>
                                                .Create(StaticData.ClusterVNode, connectionSettings, ActorConfiguration.Default)
                                                .WithReadAllFromStartCache(eventTypeProvider: tradeDataEventProvider)
                                                .Build();

                var marketDataSink = StatelessActorBuilder<MarketDataSink, DemoSystemRegistry>
                                                .Create(ActorConfiguration.Default)
                                                .WithBus<IMarketDataBus>((actor, bus) =>
                                                {
                                                    actor.SubscribeMarketDataBus();
                                                })
                                                .Build();

                var marketDataCache = marketDataSink.CurrentPrices.Connect();
                var tradeCache = tradeSink.State.AsObservableCache().Connect();

                tradeCache.PrintTradeChanges();
                marketDataCache.PrintMarketDataChanges();

            });

            Console.Read();

        }
    }
}
