using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Anabasis.Common;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Factories;
using Anabasis.EventStore.Snapshot;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;

namespace Anabasis.EventStore.Integration.Tests
{

    public class Trader : BaseEventStoreStatefulActor<CurrencyPair>
    {

        private static readonly string[] CcyPairs = { "EUR/USD", "EUR/GBP" };

        private  CancellationTokenSource _cancel;
        private Task _workProc;
        private readonly Random _rand = new();

        private TraderConfiguration _configuration;

        public Trader(TraderConfiguration traderConfiguration, IActorConfiguration actorConfiguration, IAggregateCache<CurrencyPair> eventStoreCache, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreCache, loggerFactory)
        {
            Initialize(traderConfiguration);
        }

        public Trader(TraderConfiguration traderConfiguration, IEventStoreActorConfigurationFactory eventStoreCacheFactory, IConnectionStatusMonitor<IEventStoreConnection> connectionStatusMonitor, ISnapshotStore<CurrencyPair> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, ILoggerFactory loggerFactory = null) : base(eventStoreCacheFactory, connectionStatusMonitor, snapshotStore, snapshotStrategy, loggerFactory)
        {
            Initialize(traderConfiguration);
        }

        public void Initialize(TraderConfiguration traderConfiguration)
        {
            _cancel = new CancellationTokenSource();
            _configuration = traderConfiguration;

            _workProc = Task.Run(HandleWork);

        }



        public CurrencyPairPriceChanged Next()
        {
            var mid = _rand.NextDouble() * 10;
            var spread = _rand.NextDouble() * 2;

            var topic = CcyPairs[_rand.Next(0, CcyPairs.Count())];

            var price = new CurrencyPairPriceChanged(
                ask: mid + spread,
                bid: mid - spread,
                mid: mid,
                spread: spread,
                ccyPairId: topic,
                traderId: _configuration.Name
            );

            return price;
        }


        public async Task WaitUntilConnected()
        {
            while (!State.IsConnected)
            {
                await Task.Delay(2000);
            }
        }

        private async Task HandleWork()
        {

            while (!_cancel.IsCancellationRequested)
            {

                Thread.Sleep(_configuration.PriceGenerationDelay);

                if (_cancel.IsCancellationRequested) return;

                await WaitUntilConnected();

                if (State.IsConnected)
                {
                    var changePrice = Next();

                    await this.EmitEventStore(changePrice);

                }
            }
        }

        public override void Dispose()
        {
            _cancel.Cancel();
            _workProc.Dispose();

            base.Dispose();
        }
    }
}
