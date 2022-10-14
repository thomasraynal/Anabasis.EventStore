using Anabasis.Common;
using Anabasis.Common.Configuration;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Snapshot;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Demo
{
    public class TradePriceUpdateService : SubscribeToAllStreamsEventStoreStatefulActor<Trade>
    {
        public TradePriceUpdateService(IActorConfigurationFactory actorConfigurationFactory, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, ILoggerFactory loggerFactory = null, ISnapshotStore<Trade> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, IKillSwitch killSwitch = null) : base(actorConfigurationFactory, connectionMonitor, loggerFactory, snapshotStore, snapshotStrategy, killSwitch)
        {
        }

        public TradePriceUpdateService(IActorConfiguration actorConfiguration, IConnectionStatusMonitor<IEventStoreConnection> connectionMonitor, AllStreamsCatchupCacheConfiguration catchupCacheConfiguration, IEventTypeProvider eventTypeProvider, ILoggerFactory loggerFactory = null, ISnapshotStore<Trade> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, IKillSwitch killSwitch = null) : base(actorConfiguration, connectionMonitor, catchupCacheConfiguration, eventTypeProvider, loggerFactory, snapshotStore, snapshotStrategy, killSwitch)
        {
        }

        public async Task Handle(MarketDataChanged marketDataChanged)
        {

            foreach (var trade in GetCurrents().Where(trade => trade.CurrencyPair == marketDataChanged.EntityId))
            {
                await this.EmitEventStore(new TradePriceChanged(trade.EntityId, Guid.NewGuid())
                {
                    MarketPrice = marketDataChanged.Bid,
                    PercentFromMarket = Math.Round(((trade.TradePrice - trade.MarketPrice) / trade.MarketPrice) * 100, 4)
                });

            }

        }
    }
}
