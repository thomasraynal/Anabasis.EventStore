using Anabasis.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Demo.Bus
{
    public class MarketDataBus : IMarketDataBus
    {
        private readonly Dictionary<string, IObservable<MarketData>> _prices = new();
        private readonly ConcurrentDictionary<string, Func<MarketDataBusMessage, Task>> _subscribers = new();
        private CompositeDisposable _cleanUp;

        public string BusId => $"{nameof(MarketDataBus)}{Guid.NewGuid()}";

        public bool IsConnected => true;

        public bool IsInitialized { get; internal set; } = false;

        public IConnectionStatusMonitor ConnectionStatusMonitor => throw new NotImplementedException();

        public void Dispose()
        {
            _cleanUp.Dispose();
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(HealthCheckResult.Healthy("healthcheck from MarketDataBus", new Dictionary<string, object>()
            {
                {"MarketDataBus", "ok"}
            }));
        }
        public IDisposable Subscribe(string consumerId,Func<MarketDataBusMessage, Task> subscriber)
        {
            _subscribers.AddOrUpdate(consumerId, subscriber, (key, value) =>
             {
                 return value;
             });

            return Disposable.Create(() => _subscribers.Remove(consumerId, out _));
        }

        private void SendSubscriber(MarketDataBusMessage marketDataChanged)
        {
            foreach (var subscriber in _subscribers)
            {
                subscriber.Value(marketDataChanged);
            }
        }

        public Task Initialize()
        {
            if (IsInitialized) return Task.CompletedTask;

            _cleanUp = new CompositeDisposable();

            foreach (var item in StaticData.CurrencyPairs)
            {
                _prices[item.EntityId] = GenerateMarketDataStream(item).Replay(1).RefCount();
                _cleanUp.Add(_prices[item.EntityId].Subscribe());
            }

            IsInitialized = true;

            return Task.CompletedTask;
        }

        private IObservable<MarketData> GenerateMarketDataStream(CurrencyPair currencyPair)
        {

            return Observable.Create<MarketData>(observer =>
            {
                var spread = currencyPair.DefaultSpread;
                var midRate = currencyPair.InitialPrice;
                var bid = midRate - (spread * currencyPair.PipSize);
                var offer = midRate + (spread * currencyPair.PipSize);
                var initial = new MarketData(currencyPair.EntityId, bid, offer);

                var currentPrice = initial;

                observer.OnNext(initial);

                SendSubscriber(new MarketDataBusMessage(new MarketDataChanged(currentPrice.EntityId, Guid.NewGuid())
                {
                    Bid = currentPrice.Bid,
                    Offer = currentPrice.Offer
                }));

                var random = new Random();

                return Observable.Interval(TimeSpan.FromSeconds(1 / (double)currencyPair.TickFrequency))
                   .Select(_ => random.Next(1, 5))
                   .Subscribe(pips =>
                  {
                      var adjustment = Math.Round(pips * currencyPair.PipSize, currencyPair.DecimalPlaces);
                      currentPrice = random.NextDouble() > 0.5
                                      ? currentPrice + adjustment
                                      : currentPrice - adjustment;


                      observer.OnNext(currentPrice);

                      SendSubscriber(new MarketDataBusMessage(new MarketDataChanged(currentPrice.EntityId, Guid.NewGuid())
                      {
                          Bid = currentPrice.Bid,
                          Offer = currentPrice.Offer
                      }));

                  });
            });
        }
    }
}
