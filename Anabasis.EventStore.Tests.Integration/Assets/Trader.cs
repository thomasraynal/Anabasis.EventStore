using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;

using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;

namespace Anabasis.EventStore.Integration.Tests
{

    public class Trader : SubscribeToAllStreamsEventStoreStatefulActor<CurrencyPair>
    {

        private static readonly string[] CcyPairs = { "EUR/USD", "EUR/GBP" };

        private  CancellationTokenSource _cancel;
        private Task _workProc;
        private readonly Random _rand = new();

        private TraderConfiguration _configuration;

        public Trader(IActorConfigurationFactory actorConfigurationFactory, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, ILoggerFactory loggerFactory = null, ISnapshotStore<CurrencyPair> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, IKillSwitch killSwitch = null) : base(actorConfigurationFactory, connectionMonitor, loggerFactory, snapshotStore, snapshotStrategy, killSwitch)
        {
        }

        public Trader(IActorConfiguration actorConfiguration, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, AllStreamsCatchupCacheConfiguration catchupCacheConfiguration, IEventTypeProvider eventTypeProvider, ILoggerFactory loggerFactory = null, ISnapshotStore<CurrencyPair> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, IKillSwitch killSwitch = null) : base(actorConfiguration, connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy, killSwitch)
        {
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
            while (!IsConnected)
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

                if (IsConnected)
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
