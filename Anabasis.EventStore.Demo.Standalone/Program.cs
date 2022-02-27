using Anabasis.Common;
using Anabasis.EventStore.Demo.Bus;
using Anabasis.EventStore.EventProvider;
using Anabasis.EventStore.Standalone;
using Anabasis.RabbitMQ;
using Anabasis.RabbitMQ.Connection;
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


            var rabbitMqConnectionOptions = new RabbitMqConnectionOptions()
            {
                HostName = "localhost",
                Password = "password",
                Username = "username",
            };


            Log.Logger = new LoggerConfiguration()
              .MinimumLevel.Information()
              .MinimumLevel.Override("Microsoft", LogEventLevel.Debug)
              .WriteTo.Console()
              .CreateLogger();

            var loggerFactory = new LoggerFactory().AddSerilog(Log.Logger);

            var anabasisAppContext = new AnabasisAppContext("appName", "appGroup", new Version(1, 0));

            var defaultSerializer = new DefaultSerializer();

            var connection = new RabbitMqConnection(rabbitMqConnectionOptions, anabasisAppContext, loggerFactory);

            var monitor = new RabbitMqConnectionStatusMonitor(connection, loggerFactory);


            //Task.Run(() =>
            //{

            //    var connectionSettings = StaticData.GetConnectionSettings();

            //    var tradeDataEventProvider = new DefaultEventTypeProvider<Trade>(() => new[] { typeof(TradeCreated), typeof(TradeStatusChanged) });
            //    var marketDataEventProvider = new DefaultEventTypeProvider<MarketData>(() => new[] { typeof(MarketDataChanged) });

            //    var tradeService = EventStoreStatelessActorBuilder<TradeService, DemoSystemRegistry>
            //                                    .Create(StaticData.ClusterVNode, connectionSettings, ActorConfiguration.Default)
            //                                    .WithSubscribeFromEndToAllStream()
            //                                    .Build();

            //    var tradePriceUpdateService = EventStoreStatefulActorBuilder<TradePriceUpdateService, Trade, DemoSystemRegistry>
            //                                    .Create(StaticData.ClusterVNode, connectionSettings, ActorConfiguration.Default)
            //                                    .WithReadAllFromStartCache(eventTypeProvider: tradeDataEventProvider)
            //                                    .WithSubscribeFromEndToAllStream()
            //                                    .WithBus<IMarketDataBus>((actor, bus) =>
            //                                    {
            //                                        actor.SubscribeMarketDataBus();
            //                                    })
            //                                    .Build();

            //    var tradeSink = EventStoreStatefulActorBuilder<TradeSink, Trade, DemoSystemRegistry>
            //                                    .Create(StaticData.ClusterVNode, connectionSettings, ActorConfiguration.Default)
            //                                    .WithReadAllFromStartCache(eventTypeProvider: tradeDataEventProvider)
            //                                    .Build();

            //    var marketDataSink = StatelessActorBuilder<MarketDataSink, DemoSystemRegistry>
            //                                    .Create(ActorConfiguration.Default)
            //                                    .WithBus<IMarketDataBus>((actor, bus) =>
            //                                    {
            //                                        actor.SubscribeMarketDataBus();
            //                                    })
            //                                    .Build();

            //    var marketDataCache = marketDataSink.CurrentPrices.Connect();
            //    var tradeCache = tradeSink.State.AsObservableCache().Connect();

            //    tradeCache.PrintTradeChanges();

            //    marketDataCache.PrintMarketDataChanges();

            //});

            Console.Read();

        }
    }
}
