using Anabasis.Common;
using Anabasis.EventStore.Actor;
using Anabasis.EventStore.AspNet.Factories;
using Anabasis.EventStore.Cache;
using Anabasis.EventStore.Connection;
using Anabasis.EventStore.Repository;
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
        public TradePriceUpdateService(IActorConfiguration actorConfiguration, IEventStoreAggregateRepository eventStoreRepository, IEventStoreCache<Trade> eventStoreCache, ILoggerFactory loggerFactory = null) : base(actorConfiguration, eventStoreRepository, eventStoreCache, loggerFactory)
        {
        }

        public TradePriceUpdateService(IEventStoreActorConfigurationFactory eventStoreCacheFactory, IEventStoreAggregateRepository eventStoreRepository, IConnectionStatusMonitor<IEventStoreConnection> connectionStatusMonitor,  ILoggerFactory loggerFactory = null) : base(eventStoreCacheFactory, eventStoreRepository, connectionStatusMonitor, loggerFactory)
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
