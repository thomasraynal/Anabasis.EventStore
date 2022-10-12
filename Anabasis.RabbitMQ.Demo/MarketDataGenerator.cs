using Anabasis.Common;
using Anabasis.Common.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anabasis.RabbitMQ.Demo
{
    public class MarketDataGenerator : BaseStatelessActor
    {
        private readonly Dictionary<string, IObservable<MarketData>> _prices = new();
        private CompositeDisposable _cleanUp;
        private Random _rand;
        private static readonly string[] MarketDataBus = new[] { StaticData.MarketDataBusOne, StaticData.MarketDataBusTwo };

        public MarketDataGenerator(IActorConfigurationFactory actorConfigurationFactory, ILoggerFactory loggerFactory = null) : base(actorConfigurationFactory, loggerFactory)
        {
        }

        public MarketDataGenerator(IActorConfiguration actorConfiguration, ILoggerFactory loggerFactory = null) : base(actorConfiguration, loggerFactory)
        {
        }

        public async override Task OnInitialized()
        {
            await Task.Delay(5000);

            InitializeMarketDataStream();
        }

        public void InitializeMarketDataStream()
        {
            _rand = new Random();
            _cleanUp = new CompositeDisposable();

            foreach (var item in StaticData.CurrencyPairs)
            {
                _prices[item.EntityId] = GenerateMarketDataStream(item).Replay(1).RefCount();
                _cleanUp.Add(_prices[item.EntityId].Subscribe());
            }

        }

        private string GetRandomMarketplaceBus()
        {
            var index = _rand.Next(MarketDataBus.Length);

            return MarketDataBus[index];
        }

        private IObservable<MarketData> GenerateMarketDataStream(CurrencyPair currencyPair)
        {
            var rand = new Random();

            return Observable.Create<MarketData>(observer =>
            {
                var spread = currencyPair.DefaultSpread;
                var midRate = currencyPair.InitialPrice;
                var bid = midRate - (spread * currencyPair.PipSize);
                var offer = midRate + (spread * currencyPair.PipSize);
                var initial = new MarketData(currencyPair.EntityId, bid, offer);

                var currentPrice = initial;

                observer.OnNext(initial);

                var random = new Random();

                var exchange = GetRandomMarketplaceBus();

                var marketDataChanged = new MarketDataQuoteChanged(Guid.NewGuid(), Guid.NewGuid())
                {
                    CurrencyPair = currentPrice.EntityId,
                    Bid = currentPrice.Bid,
                    Offer = currentPrice.Offer
                };

                this.EmitRabbitMq(marketDataChanged, exchange);

                Logger.LogInformation($"{marketDataChanged} - {exchange}");

                return Observable.Interval(TimeSpan.FromMilliseconds(200))
                   .Select(_ => random.Next(1, 5))
                   .Subscribe(pips =>
                   {
                       var adjustment = Math.Round(pips * currencyPair.PipSize, currencyPair.DecimalPlaces);
                       currentPrice = random.NextDouble() > 0.5
                                       ? currentPrice + adjustment
                                       : currentPrice - adjustment;


                       observer.OnNext(currentPrice);

                       var exchange = GetRandomMarketplaceBus();

                       var marketDataChanged = new MarketDataQuoteChanged(Guid.NewGuid(), Guid.NewGuid())
                       {
                           CurrencyPair = currentPrice.EntityId,
                           Bid = currentPrice.Bid,
                           Offer = currentPrice.Offer
                       };

                       this.EmitRabbitMq(marketDataChanged, exchange);

                       Logger.LogInformation($"{marketDataChanged} - {exchange}");

                   });
            });
        }
    }
}
