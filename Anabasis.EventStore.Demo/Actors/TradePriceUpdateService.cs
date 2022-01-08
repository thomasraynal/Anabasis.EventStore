using Anabasis.EventStore.Actor;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Anabasis.EventStore.Demo
{
    public class TradePriceUpdateService : BaseStatefulActor<Trade>
    {

        public TradePriceUpdateService(IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<Trade> eventStoreCache) : base(eventStoreRepository, eventStoreCache)
        {
        }

        public TradePriceUpdateService(IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<Trade> eventStoreCache, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, eventStoreCache, loggerFactory)
        {
        }

        public TradePriceUpdateService(IEventStoreAggregateRepository eventStoreRepository, IConnectionStatusMonitor connectionStatusMonitor, IEventStoreCacheFactory eventStoreCacheFactory, ILoggerFactory loggerFactory = null) : base(eventStoreRepository, connectionStatusMonitor, eventStoreCacheFactory, loggerFactory)
        {
        }

        public async Task Handle(MarketDataChanged marketDataChanged)
        {

            foreach (var trade in State.GetCurrents().Where(trade => trade.CurrencyPair == marketDataChanged.EntityId))
            {
                await EmitEventStore(new TradePriceChanged(trade.EntityId, Guid.NewGuid())
                {
                    MarketPrice = marketDataChanged.Bid,
                    PercentFromMarket = Math.Round(((trade.TradePrice - trade.MarketPrice) / trade.MarketPrice) * 100, 4)
                });

            }

        }
    }
}
