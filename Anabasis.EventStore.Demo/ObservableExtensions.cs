using DynamicData;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace Anabasis.EventStore.Demo
{
    public static class ObservableExtensions
    {
        public static IDisposable PrintMarketDataChanges(this IObservable<IChangeSet<MarketData, string>> marketDataChangeSet)
        {
            return marketDataChangeSet.Scan(CcyPairReporting.Empty, (previous, changeSet) =>
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
        }

        public static IDisposable PrintTradeChanges(this IObservable<IChangeSet<Trade, long>> observable)
        {

            return observable.Subscribe(trades =>
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

        }
    }
}

