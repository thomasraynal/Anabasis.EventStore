﻿using Microsoft.Extensions.Hosting;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Demo.AspNet
{
    public class HostedService : IHostedService
    {
        private readonly TradeSink _tradeSink;
        private readonly MarketDataSink _marketDataSink;
        private readonly CompositeDisposable _cleanUp;

        public HostedService(TradeSink tradeSink, MarketDataSink marketDataSink)
        {
            _tradeSink = tradeSink;
            _marketDataSink = marketDataSink;
            _cleanUp = new CompositeDisposable();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            var marketDataCache = _marketDataSink.CurrentPrices.Connect();
            var tradeCache = _tradeSink.AsObservableCache().Connect();

            _cleanUp.Add(tradeCache.PrintTradeChanges());
            _cleanUp.Add(marketDataCache.PrintMarketDataChanges());

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cleanUp.Dispose();
            _marketDataSink.Dispose();
            _tradeSink.Dispose();

            return Task.CompletedTask;
        }
    }
}
