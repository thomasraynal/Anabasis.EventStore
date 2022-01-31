using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Repository;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace Anabasis.EventStore.Demo
{
    public class MarketDataService : BaseEventStoreStatelessActor
    {
        private readonly Dictionary<string, IObservable<MarketData>> _prices = new();

        public MarketDataService(IActorConfiguration actorConfiguration, IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory = null) : base(actorConfiguration,eventStoreRepository, loggerFactory)
        {
            Initialize();
        }

        public MarketDataService(IActorConfigurationFactory actorConfigurationFactory, IEventStoreRepository eventStoreRepository, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, eventStoreRepository, loggerFactory)
        {
            Initialize();
        }

        private void Initialize()
        {
            foreach (var item in StaticData.CurrencyPairs)
            {
                _prices[item.EntityId] = GenerateStream(item).Replay(1).RefCount();
                _prices[item.EntityId].Subscribe();
            }
        }

        private IObservable<MarketData> GenerateStream(CurrencyPair currencyPair)
        {

            return Observable.Create<MarketData>((Func<IObserver<MarketData>, System.Threading.Tasks.Task<IDisposable>>)(async observer =>
           {
               var spread = currencyPair.DefaultSpread;
               var midRate = currencyPair.InitialPrice;
               var bid = midRate - (spread * currencyPair.PipSize);
               var offer = midRate + (spread * currencyPair.PipSize);
               var initial = new MarketData((string)currencyPair.EntityId, bid, offer);

               var currentPrice = initial;

               observer.OnNext(initial);

               await EmitEventStore(new MarketDataChanged((string)currentPrice.EntityId, Guid.NewGuid())
               {
                   Bid = currentPrice.Bid,
                   Offer = currentPrice.Offer
               });

               var random = new Random();

         //for a given period, move prices by up to 5 pips
         return Observable.Interval(TimeSpan.FromSeconds(1 / (double)currencyPair.TickFrequency))
            .Select(_ => random.Next(1, 5))
            .Subscribe((Action<int>)(async pips =>
            {
               //move up or down between 1 and 5 pips
               var adjustment = Math.Round(pips * currencyPair.PipSize, currencyPair.DecimalPlaces);
                     currentPrice = random.NextDouble() > 0.5
                                           ? currentPrice + adjustment
                                           : currentPrice - adjustment;


                     observer.OnNext(currentPrice);

                     await EmitEventStore(new MarketDataChanged((string)currentPrice.EntityId, Guid.NewGuid())
                     {
                         Bid = currentPrice.Bid,
                         Offer = currentPrice.Offer
                     });

                 }));
           }));
        }

    }
}
