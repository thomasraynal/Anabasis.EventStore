using Anabasis.Common;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Factories;
using Anabasis.EventStore.Repository;
using Anabasis.EventStore.Snapshot;
using EventStore.ClientAPI;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Demo
{
    public class TradePriceUpdateService : BaseEventStoreStatefulActor<Trade>
    {
        public TradePriceUpdateService(IActorConfiguration actorConfiguration, IAggregateCache<Trade> eventStoreCache, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreCache, loggerFactory)
        {
        }

        public TradePriceUpdateService(IEventStoreActorConfigurationFactory eventStoreCacheFactory, IConnectionStatusMonitor<IEventStoreConnection> connectionStatusMonitor, ISnapshotStore<Trade> snapshotStore = null, ISnapshotStrategy snapshotStrategy = null, ILoggerFactory loggerFactory = null) : base(eventStoreCacheFactory, connectionStatusMonitor, snapshotStore, snapshotStrategy, loggerFactory)
        {
        }

        public async Task Handle(MarketDataChanged marketDataChanged)
        {

            foreach (var trade in State.GetCurrents().Where(trade => trade.CurrencyPair == marketDataChanged.EntityId))
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
