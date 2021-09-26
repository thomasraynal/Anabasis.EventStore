using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Repository;
using DynamicData.Kernel;

namespace Anabasis.EventStore.Demo
{
  public class MarketDataService : BaseStatelessActor
  {
    private readonly Dictionary<string, IObservable<MarketData>> _prices = new Dictionary<string, IObservable<MarketData>>();

    public MarketDataService(IStaticData staticData, IEventStoreRepository eventStoreRepository) : base(eventStoreRepository)
    {

      foreach (var item in staticData.CurrencyPairs)
      {
        _prices[item.EntityId] = GenerateStream(item).Replay(1).RefCount();
        _prices[item.EntityId].Subscribe();
      }

    }

    private IObservable<MarketData> GenerateStream(CurrencyPair currencyPair)
    {

      return Observable.Create<MarketData>(async observer =>
     {
       var spread = currencyPair.DefaultSpread;
       var midRate = currencyPair.InitialPrice;
       var bid = midRate - (spread * currencyPair.PipSize);
       var offer = midRate + (spread * currencyPair.PipSize);
       var initial = new MarketData(currencyPair.EntityId, bid, offer);

       var currentPrice = initial;

       observer.OnNext(initial);

       await Emit(new MarketDataChanged(currentPrice.EntityId, Guid.NewGuid())
       {
         Bid = currentPrice.Bid,
         Offer = currentPrice.Offer
       });

       var random = new Random();

        //for a given period, move prices by up to 5 pips
        return Observable.Interval(TimeSpan.FromSeconds(1 / (double)currencyPair.TickFrequency))
           .Select(_ => random.Next(1, 5))
           .Subscribe(async pips =>
           {
              //move up or down between 1 and 5 pips
              var adjustment = Math.Round(pips * currencyPair.PipSize, currencyPair.DecimalPlaces);
             currentPrice = random.NextDouble() > 0.5
                                         ? currentPrice + adjustment
                                         : currentPrice - adjustment;


             observer.OnNext(currentPrice);

             await Emit(new MarketDataChanged(currentPrice.EntityId, Guid.NewGuid())
             {
               Bid = currentPrice.Bid,
               Offer = currentPrice.Offer
             });

           });
     });
    }

  }
}
