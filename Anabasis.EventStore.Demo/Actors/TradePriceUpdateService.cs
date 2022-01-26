using Anabasis.Common;
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
    public class TradePriceUpdateService : BaseEventStoreStatefulActor<Trade>
    {

        public TradePriceUpdateService(IActorConfiguration actorConfiguration, IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<Trade> eventStoreCache) : base(actorConfiguration, eventStoreRepository, eventStoreCache)
        {
        }

        public TradePriceUpdateService(IActorConfiguration actorConfiguration, IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<Trade> eventStoreCache, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, eventStoreCache, loggerFactory)
        {
        }

        public TradePriceUpdateService(IActorConfiguration actorConfiguration, IEventStoreAggregateRepository eventStoreRepository, IConnectionStatusMonitor connectionStatusMonitor, IEventStoreCacheFactory eventStoreCacheFactory, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, connectionStatusMonitor, eventStoreCacheFactory, loggerFactory)
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
