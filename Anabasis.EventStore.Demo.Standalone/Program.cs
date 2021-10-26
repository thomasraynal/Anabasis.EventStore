using Anabasis.EventStore.Actor;
using Anabasis.EventStore.EventProvider;
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

    class Program
    {

        static void Main(string[] args)
        {

            Task.Run( () =>
            {

                var connectionSettings = StaticData.GetConnectionSettings();

                var marketDataService = StatelessActorBuilder<MarketDataService, DemoSystemRegistry>.Create(StaticData.ClusterVNode, connectionSettings)
                                                     .Build();

                var tradeService = StatelessActorBuilder<TradeService, DemoSystemRegistry>.Create(StaticData.ClusterVNode, connectionSettings)
                                                .WithSubscribeFromEndToAllQueue()
                                                .Build();

                var tradeDataEventProvider = new DefaultEventTypeProvider<long, Trade>(() => new[] { typeof(TradeCreated), typeof(TradeStatusChanged) });

                var tradePriceUpdateService = StatefulActorBuilder<TradePriceUpdateService, long, Trade, DemoSystemRegistry>
                                                .Create(StaticData.ClusterVNode, connectionSettings)
                                                .WithReadAllFromStartCache(eventTypeProvider: tradeDataEventProvider)
                                                .WithSubscribeFromEndToAllQueue()
                                                .Build();

                var tradeSink = StatefulActorBuilder<TradeSink, long, Trade, DemoSystemRegistry>
                                               .Create(StaticData.ClusterVNode, connectionSettings)
                                               .WithReadAllFromStartCache(eventTypeProvider: tradeDataEventProvider)
                                               .Build();

                var marketDataEventProvider = new DefaultEventTypeProvider<string, MarketData>(() => new[] { typeof(MarketDataChanged) });
                var marketDataSink = StatefulActorBuilder<MarketDataSink, string, MarketData, DemoSystemRegistry>
                                              .Create(StaticData.ClusterVNode, connectionSettings)
                                              .WithReadAllFromStartCache(eventTypeProvider: marketDataEventProvider)
                                              .Build();

                var marketDataCache = marketDataSink.State.AsObservableCache().Connect();
                var tradeCache = tradeSink.State.AsObservableCache().Connect();

                tradeCache.Subscribe(trades =>
                  {

                      foreach (var tradeChange in trades)
                      {

                          const string messageTemplate = "[{5}] => {0} {1} {2} ({4}). Status = {3}";

                          var trade = tradeChange.Current;

                          Console.WriteLine(string.Format(messageTemplate,
                                                              trade.BuyOrSell,
                                                              trade.Amount,
                                                              trade.CurrencyPair,
                                                              trade.Status,
                                                              trade.Customer,
                                                              tradeChange.Reason));
                      }

                  });


                marketDataCache.Scan(CcyPairReporting.Empty, (previous, changeSet) =>
           {

               foreach (var change in changeSet)
               {
                   var isUp = previous.ContainsKey(change.Key) && change.Current.Offer > previous[change.Key].offer;

                   previous[change.Key] = (change.Current.Bid, change.Current.Offer, (change.Current.Offer - change.Current.Bid), isUp);
               }

               return new CcyPairReporting(previous);

           })
          .Sample(TimeSpan.FromSeconds(5))
          .Subscribe(ccyPairReporting =>
          {

              var reportings = ccyPairReporting.Select(ccyPair =>
        {
            var upDown = ccyPair.Value.IsUp ? "UP  " : "DOWN";

            return $"[{ccyPair.Key}] => {upDown} {ccyPair.Value.offer}/{ccyPair.Value.offer} ";

        }).ToArray();


              var bufferRightLast = "*";
              var bufferLeft = "     *";
              var spaceLeft = bufferLeft.Length;
              var maxReportingLength = reportings.Max(reporting => reporting.Length);

              var bar = string.Concat(Enumerable.Range(0, maxReportingLength + bufferLeft.Length + bufferRightLast.Length).Select(index => index < spaceLeft ? " " : "*").ToArray());

              Console.WriteLine(bar);

              foreach (var reportingLine in reportings)
              {
                  var bufferLength = (maxReportingLength + bufferLeft.Length) - reportingLine.Length;
                  var bufferRight = bufferLength == 0 ? bufferLeft : string.Concat(Enumerable.Range(0, bufferLength - spaceLeft).Select(_ => " ").ToArray()) + bufferRightLast;

                  Console.WriteLine($"{bufferLeft}{reportingLine}{bufferRight}");
              }

              Console.WriteLine(bar);
          });

            });

            Console.Read();

        }
    }
}
