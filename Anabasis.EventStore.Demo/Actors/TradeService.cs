using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Repository;
using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Demo
{
    public class TradeService : BaseStatelessActor, IDisposable
    {
        private readonly Random _random = new();
        private readonly IDictionary<string, MarketData> _latestPrices = new Dictionary<string, MarketData>();
        private readonly object _locker = new();
        private int _counter = 0;
        private IDisposable _cleanup;

        public TradeService(IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, loggerFactory)
        {

            var tradesData = GenerateTradesAndMaintainCache().Publish();

            _cleanup = tradesData.Connect();
        }

        public Task Handle(MarketDataChanged marketDataChanged)
        {
            _latestPrices[marketDataChanged.EntityId] = new MarketData(marketDataChanged.EntityId, marketDataChanged.Bid, marketDataChanged.Offer);

            return Task.CompletedTask;
        }

        public IEnumerable<Trade> Generate(int numberToGenerate, bool initialLoad = false)
        {
            Trade NewTrade()
            {
                var id = _counter++;
                var bank = StaticData.Customers[_random.Next(0, StaticData.Customers.Length)];
                var pair = StaticData.CurrencyPairs[_random.Next(0, StaticData.CurrencyPairs.Length)];
                var amount = (_random.Next(1, 2000) / 2) * (10 ^ _random.Next(1, 5));
                var buySell = _random.Next(0, 2) == 1 ? BuyOrSell.Buy : BuyOrSell.Sell;

                if (initialLoad)
                {
                    var status = _random.NextDouble() > 0.5 ? TradeStatus.Live : TradeStatus.Closed;
                    var seconds = _random.Next(1, 60 * 60 * 24);
                    var time = DateTime.Now.AddSeconds(-seconds);
                    return new Trade(id, bank, pair.EntityId, status, buySell, GererateRandomPrice(pair, buySell), amount, timeStamp: time);
                }
                return new Trade(id, bank, pair.EntityId, TradeStatus.Live, buySell, GererateRandomPrice(pair, buySell), amount);
            }


            IEnumerable<Trade> result;
            lock (_locker)
            {
                result = Enumerable.Range(1, numberToGenerate).Select(_ => NewTrade()).ToArray();
            }
            return result;
        }

        private decimal GererateRandomPrice(CurrencyPair currencyPair, BuyOrSell buyOrSell)
        {

            var price = _latestPrices.Lookup(currencyPair.EntityId)
                                .ConvertOr(md => md.Bid, () => currencyPair.InitialPrice);

            //generate percent price 1-100 pips away from the inital market
            var pipsFromMarket = _random.Next(1, 100);
            var adjustment = Math.Round(pipsFromMarket * currencyPair.PipSize, currencyPair.DecimalPlaces);
            return buyOrSell == BuyOrSell.Sell ? price + adjustment : price - adjustment;
        }

        private IObservable<IChangeSet<Trade, string>> GenerateTradesAndMaintainCache()
        {

            return ObservableChangeSet.Create<Trade, string>(async cache =>
            {

                var random = new Random();

                var initialTrades = Generate(50, true);

                cache.AddOrUpdate(initialTrades);

                foreach (var trade in initialTrades)
                {
                    await EmitEventStore(new TradeCreated(trade.EntityId, Guid.NewGuid())
                    {
                        Amount = trade.Amount,
                        BuyOrSell = trade.BuyOrSell,
                        CurrencyPair = trade.CurrencyPair,
                        Customer = trade.Customer,
                        MarketPrice = trade.MarketPrice,
                        Status = trade.Status,
                        PercentFromMarket = trade.PercentFromMarket,
                        TradePrice = trade.TradePrice
                    });
                }

                TimeSpan RandomInterval() => TimeSpan.FromMilliseconds(random.Next(2500, 5000));

          // create a random number of trades at a random interval
          var tradeGenerator = TaskPoolScheduler.Default
              .ScheduleRecurringAction(RandomInterval, async () =>
              {
                      var number = random.Next(1, 5);
                      var trades = Generate(number);

                      cache.AddOrUpdate(trades);

                      foreach (var trade in trades)
                      {
                          await EmitEventStore(new TradeCreated(trade.EntityId, Guid.NewGuid())
                          {
                              Amount = trade.Amount,
                              BuyOrSell = trade.BuyOrSell,
                              CurrencyPair = trade.CurrencyPair,
                              Customer = trade.Customer,
                              MarketPrice = trade.MarketPrice,
                              Status = trade.Status,
                              PercentFromMarket = trade.PercentFromMarket,
                              TradePrice = trade.TradePrice
                          });
                      }

                  });

          // close a random number of trades at a random interval
          var tradeCloser = TaskPoolScheduler.Default
              .ScheduleRecurringAction(RandomInterval, () =>
              {
                      var number = random.Next(1, 2);


                      cache.Edit(innerCache =>
                {
                        var trades = innerCache.Items
                                  .Where(trade => trade.Status == TradeStatus.Live)
                                  .OrderBy(t => Guid.NewGuid()).Take(number).ToArray();

                        var toClose = trades.Select(trade => new Trade(trade, TradeStatus.Closed));

                        cache.AddOrUpdate(toClose);

                        foreach (var trade in toClose)
                        {
                            EmitEventStore(new TradeStatusChanged(trade.EntityId, Guid.NewGuid())
                            {
                                Status = trade.Status
                            }).Wait();

                        }
                    });
                  });

          //expire closed items from the cache to avoid unbounded data
          var expirer = cache
              .ExpireAfter(t => t.Status == TradeStatus.Closed ? TimeSpan.FromMinutes(1) : (TimeSpan?)null, TimeSpan.FromMinutes(1), TaskPoolScheduler.Default)
              .Subscribe();

                return new CompositeDisposable(tradeGenerator, tradeCloser, expirer);

            }, trade => trade.EntityId);
        }

        public override void Dispose()
        {
            _cleanup.Dispose();
        }
    }
}
