using System;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Anabasis.Actor;
using DynamicData;
using DynamicData.Kernel;


namespace Anabasis.EventStore.Demo
{
  public class TradeService : BaseActor, ITradeService, IDisposable
  {
    private readonly TradeGenerator _tradeGenerator;

    private readonly IDisposable _cleanup;

    public TradeService(TradeGenerator tradeGenerator, IEventStoreRepository eventStoreRepository) : base(eventStoreRepository)
    {
      _tradeGenerator = tradeGenerator;

      //emulate a trade service which asynchronously 
      var tradesData = GenerateTradesAndMaintainCache().Publish();

      //call AsObservableCache() so the cache can be directly exposed
      All = tradesData.AsObservableCache();

      //create a derived cache  
      Live = tradesData.Filter(trade => trade.Status == TradeStatus.Live).AsObservableCache();

      //log changes
      var loggerWriter = LogChanges();

      _cleanup = new CompositeDisposable(All, tradesData.Connect(), loggerWriter);
    }

    private IObservable<IChangeSet<Trade, long>> GenerateTradesAndMaintainCache()
    {
      //construct an cache datasource specifying that the primary key is Trade.Id
      return ObservableChangeSet.Create<Trade, long>(async cache =>
      {
        /*
            The following code emulates an external trade provider. 
            Alternatively you can use "new SourceCacheTrade, long>(t=>t.Id)" and manually maintain the cache.

            For examples of creating a observable change sets, see https://github.com/RolandPheasant/DynamicData.Snippets
        */

        //bit of code to generate trades
        var random = new Random();

        var initialTrades = _tradeGenerator.Generate(50, true);

        //initally load some trades 
        cache.AddOrUpdate(initialTrades);

        foreach (var trade in initialTrades)
        {
          await Emit(new TradeCreated(trade.EntityId, Guid.NewGuid())
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
             var trades = _tradeGenerator.Generate(number);

             cache.AddOrUpdate(trades);

             foreach (var trade in trades)
             {
               await Emit(new TradeCreated(trade.EntityId, Guid.NewGuid())
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
                              Emit(new TradeStatusChanged(trade.EntityId, Guid.NewGuid())
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

    private IDisposable LogChanges()
    {
      const string messageTemplate = "{0} {1} {2} ({4}). Status = {3}";
      return All.Connect().Skip(1)
                      .WhereReasonsAre(ChangeReason.Add, ChangeReason.Update)
                      .Cast(trade => string.Format(messageTemplate,
                                              trade.BuyOrSell,
                                              trade.Amount,
                                              trade.CurrencyPair,
                                              trade.Status,
                                              trade.Customer))
                      .Subscribe();

    }

    public IObservableCache<Trade, long> All { get; }

    public IObservableCache<Trade, long> Live { get; }

    public override void Dispose()
    {
      _cleanup.Dispose();

      base.Dispose();
    }
  }
}
